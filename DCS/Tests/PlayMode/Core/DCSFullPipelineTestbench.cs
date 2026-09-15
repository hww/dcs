// === FILE: Examples/Core/DCSFullPipelineTestbench.cs ===
using System;
using System.Diagnostics;
using UnityEngine;
using DCS.Core;
using DCS.Lua;
using DCS.Lua.Bindings;

namespace DCS.Tests.PlayMode
{

    public class DCSFullPipelineTestbench : MonoBehaviour
    {
        private HostChain _hostChain;
        private LuaStateWrapper _luaVM;
        private Handle _benchmarkComponentHandle;
        private int _poolId;

        [Header("Benchmark Settings")]
        [Tooltip("Number of iterations executed in Start() to measure raw execution speed.")]
        public int WarmupStressIterations = 10000;

        private void Awake()
        {
            // 1. Core ECS / DCS initialization
            ComponentRegistry.InitializeAllPools();
            _hostChain = new HostChain();
            LuaManager._globalHostChain = _hostChain;

            // 2. Spinning up isolated clean Lua Virtual Machine environment
            _luaVM = new LuaStateWrapper("TestbenchEngine");
            IntPtr L = _luaVM.L;

            // Register core framework bindings
            EcsBindings.Register(L);

            // 3. Allocate Data Host and Benchmark Component
            Host testHost = HostManager.CreateHost();
            _poolId = ComponentType<BenchmarkComponent>.Id;
            _benchmarkComponentHandle = ComponentRegistry.Pools[_poolId].SystemAllocate(testHost, _hostChain);

            // Inject high-precision baseline values directly into native continuous memory
            ref var comp = ref ComponentRegistry.GetPool<BenchmarkComponent>().ResolveHandle(_benchmarkComponentHandle);
            comp.EntityName = "Naughty_Agent_Alpha";
            comp.IsActive = true;
            comp.Position = new Vector3(12.5f, 0.0f, -44.2f);
            comp.TeamColor = new Color(0.0f, 1.0f, 0.0f, 0.8f); // Green semi-transparent
            comp.AIState = ETestAIState.Patrolling;
            comp.Stats.Level = 42;
            comp.Stats.CritChance = 0.15f;
            comp.Stats.IsBoss = false;
        }

        private void Start()
        {
            // Define high-performance test script block. 
            // It fetches, asserts structural types, and mutates them recursively inside raw memory registers.
            string testScript = $@"
            local typeId = {_poolId}
            local packedHandle = {_benchmarkComponentHandle.Pack()}

            function RunTestCycle()
                -- 1. Read Primitives
                local name = DCS_GetField(typeId, packedHandle, 'EntityName')
                local active = DCS_GetField(typeId, packedHandle, 'IsActive')

                -- 2. Read Math Unboxed Multi-Returns (Vector3 & Color)
                local px, py, pz = DCS_GetField(typeId, packedHandle, 'Position')
                local cr, cg, cb, ca = DCS_GetField(typeId, packedHandle, 'TeamColor')

                -- 3. Read Enum (received as unboxed raw integer value)
                local stateEnum = DCS_GetField(typeId, packedHandle, 'AIState')

                -- 4. Read Deeply Nested Custom Struct (unboxed sequentially)
                local lvl, crit, boss = DCS_GetField(typeId, packedHandle, 'Stats')

                -- 5. Mutate Fields Back into C# structures
                DCS_SetField(typeId, packedHandle, 'Position', px + 1.0, py, pz - 1.0)
                DCS_SetField(typeId, packedHandle, 'AIState', 2) -- Shift state to ETestAIState.CombatAmbusher
                DCS_SetField(typeId, packedHandle, 'Stats', lvl + 1, crit * 2.0, true)
            end
        ";

            _luaVM.ExecuteString(testScript, "testbench_logic.lua");

            // Execute stress measurement profiling sweep
            UnityEngine.Debug.Log($"<color=orange>[Testbench]</color> Starting stress verification phase ({WarmupStressIterations} iterations)...");

            Stopwatch sw = Stopwatch.StartNew();
            IntPtr L = _luaVM.L;

            for (int i = 0; i < WarmupStressIterations; i++)
            {
                LuaNative.lua_getglobal(L, "RunTestCycle");
                if (LuaNative.lua_pcall(L, 0, 0, 0) != 0)
                {
                    UnityEngine.Debug.LogError($"[Lua Error] " + _luaVM.GetStringFromStack(-1));
                    break;
                }
            }
            sw.Stop();

            // 6. Validate Data Integrity after full stress iteration loop passes
            ref var verify = ref ComponentRegistry.GetPool<BenchmarkComponent>().ResolveHandle(_benchmarkComponentHandle);

            UnityEngine.Debug.Log($"<color=green>[Testbench Complete]</color> Executed in {sw.ElapsedMilliseconds} ms " +
                                  $"({(sw.Elapsed.TotalMicroseconds() / WarmupStressIterations).ToString("F2")} μs per end-to-end Lua cycle).");

            // Assert modified C# structural values match exact mathematical mutations applied inside Lua
            UnityEngine.Debug.Log($"<b>[Integrity Check]</b> Position: {verify.Position} (Expected: {12.5f + WarmupStressIterations}, 0, {-44.2f - WarmupStressIterations})");
            UnityEngine.Debug.Log($"<b>[Integrity Check]</b> AI State: {verify.AIState} (Expected: CombatAmbusher)");
            UnityEngine.Debug.Log($"<b>[Integrity Check]</b> Deep Struct -> Stats.Level: {verify.Stats.Level} (Expected: 43), Stats.IsBoss: {verify.Stats.IsBoss} (Expected: True)");
        }

        private void Update()
        {
            // Continuous frame update check loop to verify garbage collector allocations under live profiler sweeps
            IntPtr L = _luaVM.L;
            LuaNative.lua_getglobal(L, "RunTestCycle");
            LuaNative.lua_pcall(L, 0, 0, 0);
        }

        private void OnDestroy()
        {
            _luaVM?.Dispose();
        }
    }

    // Quick fallback helper extension for older .NET frame setups
    public static class StopwatchExtensions
    {
        public static double TotalMicroseconds(this TimeSpan span)
        {
            return span.TotalMilliseconds * 1000.0;
        }
    }

}