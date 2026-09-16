using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DCS.Core;
using DCS.Lua;
using DCS.Lua.Bindings;
using System;

namespace DCS.Tests.PlayMode
{
    [TestFixture]
    public class FieldMarshallingTests
    {
        private LuaStateWrapper _lua;
        private Host _host;
        private Handle _handle;
        private int _typeId;

        [SetUp]
        public void Setup()
        {
            ComponentRegistry.InitializeAllPools();
            var chain = new HostChain();
            LuaManager._globalHostChain = chain;

            _lua = new LuaStateWrapper("FieldMarshallingTests");
            DcsBindings.Register(_lua.L);

            _host = HostManager.CreateHost();
            _typeId = ComponentType<BenchmarkComponent>.Id;
            _handle = ComponentRegistry.Pools[_typeId].SystemAllocate(_host, chain);
        }

        [TearDown]
        public void Teardown()
        {
            _lua?.Dispose();
        }

        [Test]
        public void GetField_Float_ReadsCorrectValue()
        {
            // Arrange: пишем в C#
            ref var comp = ref ComponentRegistry.GetPool<BenchmarkComponent>().ResolveHandle(_handle);
            comp.Position = new Vector3(10f, 20f, 30f);

            // Act: читаем из Lua
            string script = $@"
                local x, y, z = DCS_GetField({_typeId}, {_handle.Pack()}, 'Position')
                _G.__x, _G.__y, _G.__z = x, y, z
            ";
            _lua.ExecuteString(script, "read.lua");

            // Assert: читаем из Lua-стейта
            float x = GetGlobalFloat("__x");
            float y = GetGlobalFloat("__y");
            float z = GetGlobalFloat("__z");

            Assert.AreEqual(10f, x, 0.001f);
            Assert.AreEqual(20f, y, 0.001f);
            Assert.AreEqual(30f, z, 0.001f);
        }

        [Test]
        public void SetField_Float_WritesCorrectValue()
        {
            // Arrange
            ref var comp = ref ComponentRegistry.GetPool<BenchmarkComponent>().ResolveHandle(_handle);
            comp.Position = Vector3.zero;

            // Act: пишем из Lua
            string script = $@"
                DCS_SetField({_typeId}, {_handle.Pack()}, 'Position', 1.5, 2.5, 3.5)
            ";
            _lua.ExecuteString(script, "write.lua");

            // Assert: читаем из C#
            ref var result = ref ComponentRegistry.GetPool<BenchmarkComponent>().ResolveHandle(_handle);
            Assert.AreEqual(1.5f, result.Position.x, 0.001f);
            Assert.AreEqual(2.5f, result.Position.y, 0.001f);
            Assert.AreEqual(3.5f, result.Position.z, 0.001f);
        }

        [Test]
        public void SetField_Enum_WritesCorrectValue()
        {
            string script = $@"DCS_SetField({_typeId}, {_handle.Pack()}, 'AIState', 2)";
            _lua.ExecuteString(script, "write_enum.lua");

            ref var result = ref ComponentRegistry.GetPool<BenchmarkComponent>().ResolveHandle(_handle);
            Assert.AreEqual(ETestAIState.CombatAmbusher, result.AIState);
        }

        [Test]
        public void SetField_Nested_WritesCorrectValue()
        {
            string script = $@"DCS_SetField({_typeId}, {_handle.Pack()}, 'Stats', 99, 0.5, true)";
            _lua.ExecuteString(script, "write_nested.lua");

            ref var result = ref ComponentRegistry.GetPool<BenchmarkComponent>().ResolveHandle(_handle);
            Assert.AreEqual(99, result.Stats.Level);
            Assert.IsTrue(result.Stats.IsBoss);
        }

        private float GetGlobalFloat(string name)
        {
            IntPtr L = _lua.L;
            LuaNative.lua_getglobal(L, name);
            float v = (float)LuaNative.lua_tonumberx(L, -1, IntPtr.Zero);
            LuaNative.lua_settop(L, -2);
            return v;
        }
    }
}