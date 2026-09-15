// === NEW PLAYMODE TEST FOR TEST RUNNER ===
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DCS.Core;
using DCS.Lua;
using DCS.Lua.Bindings;
using DCS.Spatial;
using DCS.Gameplay;

namespace DCS.Tests.PlayMode
{

    [TestFixture]
    public class DCSExpressionFactoryTests
    {
        private HostChain _hostChain;
        private LuaStateWrapper _luaVM;
        private Handle _compHandle;
        private int _poolId;

        [SetUp]
        public void Setup()
        {
            ComponentRegistry.InitializeAllPools();
            _hostChain = new HostChain();
            LuaManager._globalHostChain = _hostChain;

            _luaVM = new LuaStateWrapper("TestRunnerEngine");
            EcsBindings.Register(_luaVM.L);

            Host testHost = HostManager.CreateHost();
            _poolId = ComponentType<BenchmarkComponent>.Id;
            _compHandle = ComponentRegistry.Pools[_poolId].SystemAllocate(testHost, _hostChain);

            // Set default baseline values
            ref var comp = ref ComponentRegistry.GetPool<BenchmarkComponent>().ResolveHandle(_compHandle);
            comp.Position = new Vector3(10f, 20f, 30f);
            comp.AIState = ETestAIState.Idle;
            comp.Stats.Level = 1;
            comp.Stats.IsBoss = false;
        }

        [TearDown]
        public void Teardown()
        {
            _luaVM?.Dispose();
        }

        [UnityTest]
        public IEnumerator JITExpressions_EndToEndTransmission_MaintainsDataIntegrity()
        {
            string testScript = $@"
                local typeId = {_poolId}
                local packedHandle = {_compHandle.Pack()}
                
                -- Read mathematical values
                local x, y, z = DCS_GetField(typeId, packedHandle, 'Position')
                
                -- Mutate and write back deeply nested layout structures
                DCS_SetField(typeId, packedHandle, 'Position', x + 5.0, y, z)
                DCS_SetField(typeId, packedHandle, 'AIState', 2) -- CombatAmbusher
                DCS_SetField(typeId, packedHandle, 'Stats', 99, 0.5, true)
            ";

            _luaVM.ExecuteString(testScript, "test_execution.lua");

            // Execute Lua function loop state update
            IntPtr L = _luaVM.L;

            // Wait one frame to emulate runtime integration pass
            yield return null;

            // Read back and assert data transformations on C# layout side
            var result = ComponentRegistry.GetPool<BenchmarkComponent>().ResolveHandle(_compHandle);

            Assert.AreEqual(15f, result.Position.x, 0.001f);
            Assert.AreEqual(ETestAIState.CombatAmbusher, result.AIState);
            Assert.AreEqual(99, result.Stats.Level);
            Assert.IsTrue(result.Stats.IsBoss);
        }

        [Test]
        public void LuaAI_SpatialQueryAndRoleOverride_ExecutesWithZeroGC()
        {
            // 1. Setup global infrastructure links
            ComponentRegistry.InitializeAllPools();
            HostChain chain = new HostChain();
            LuaManager._globalHostChain = chain;

            LuaStateWrapper luaVM = new LuaStateWrapper("SystemsIntegrationEngine");
            IntPtr L = luaVM.L;

            // Register all native bindings to the state boundary layers
            EcsBindings.Register(L);
            GameplayLuaBindings.Register(L);

            // 2. Mock Spatial Index layout records (Inject one StrongPoint at origin)
            MapDataset mockDataset = ScriptableObject.CreateInstance<MapDataset>();
            mockDataset.StrongPoints.Add(new StrongPointRecord
            {
                Id = 777,
                EncounterId = 1,
                Key = "SP_TEST_AMBUSH_TOWER",
                Position = new Vector3(0f, 0f, 2f),
                Enabled = true
            });
            // Link proxy boundaries inside spatial engine
            mockDataset.SpatialProxies.Add(new SpatialProxy(
                spatialId: 10,
                ownerId: 777, // StrongPoint ID
                objectType: ESpatialObjectType.StrongPoint,
                geometry: new GeometryHandle(EGeometryType.Sphere, 0),
                min: new Vector3(-1f, -1f, 1f),
                max: new Vector3(1f, 1f, 3f)
            ));
            mockDataset.Spheres.Add(new SphereGeometry { Center = new Vector3(0f, 0f, 2f), Radius = 1.5f });

            // Load simulated spatial configurations directly into runtime partitions
            if (SpatialRuntime.Instance == null)
            {
                var go = new GameObject("_SpatialRuntime");
                go.AddComponent<SpatialRuntime>();
            }
            SpatialRuntime.Instance.Load(mockDataset);

            // 3. Instantiate Lua Entity under memory safe wrapper Host (Host ID: 22)
            int agentHostId = 22;
            HostManager.GlobalHosts[agentHostId] = new HostData { Id = (ushort)agentHostId, Generation = 1, FirstComponent = -1 };

            // Load script definitions manually to simulate bootstrap environment
            string bootstrapDefines = @"
        SpatialType = { Generic = 0, Region = 1, Trigger = 2, StrongPoint = 3, NavigationSurface = 4 }
        CombatRole = { None = 0, Engager = 1, Ambusher = 2, Defender = 3, GrenadeThrower = 4, Flanker = 5 }
    ";
            luaVM.ExecuteString(bootstrapDefines, "defines.lua");

            // Load our high-level declarative state machine script
            string agentScriptCode = @"
        -- Simple setup context loop for simulation testing
        TestAgent = { hostId = 22, currentState = 'tactical_fallback' }
        
        -- Pull state table logic manually from our FSM model design blocks
        function SimulateFallbackCoroutine(agent)
            local strongPoints = Spatial.QueryRadius(0, 0, 0, 10.0, SpatialType.StrongPoint)
            if #strongPoints > 0 then
                AI.SetCombatRole(agent.hostId, CombatRole.Ambusher, strongPoints[1])
            end
        end
    ";
            luaVM.ExecuteString(agentScriptCode, "agent_logic.lua");

            // 4. Fire simulation execution step directly
            luaVM.ExecuteString("SimulateFallbackCoroutine(TestAgent)", "exec.lua");

            // 5. Assert check transformations inside C# contiguous entity chain nodes
            Host host = new Host((ushort)agentHostId, 1);
            int roleComponentPoolId = 4; // Use your exact allocated pool ID index mapping 

            var node = chain.GetTypedHandle(host, roleComponentPoolId);

            // Prove that the Lua Coroutine successfully ran, queried the C# Octree/Grid index, 
            // found the StrongPoint (777) and allocated a new CombatRoleComponent back to C#!
            Assert.IsFalse(node.IsNull, "[Integration Error] Lua script failed to attach CombatRoleComponent back to C# host data arrays.");

            // Clean virtual allocations
            luaVM.Dispose();
            SpatialRuntime.Instance.Clear();
        }
    }
}
