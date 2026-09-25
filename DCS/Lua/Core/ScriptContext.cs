using DCS.Core;
using DCS.Lua.Bindings;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
namespace DCS.Lua
{
    /// <summary>
    /// Isolated Lua state for a single location/zone.
    /// Provides only Zone.SetFact; no global DCS bindings.
    /// </summary>
    public class ScriptContext : IDisposable
    {
        private readonly LuaStateWrapper _lua;
        private readonly string _zoneName;

        // Static lookup to avoid capturing `this` in the delegate.
        private static Func<string, BaseFacts> _entityRegistryLookup;

        public ScriptContext(string zoneName, Func<string, BaseFacts> registryLookup)
        {
            _zoneName = zoneName;
            _entityRegistryLookup = registryLookup;
            _lua = new LuaStateWrapper(zoneName);
            RegisterZoneAPI();
        }

        private void RegisterZoneAPI()
        {
            IntPtr L = _lua.L;
            LuaBindings.RegisterNamespace(L, "Zone", (state, tableIndex) =>
            {
                LuaBindings.RegisterMethod(state, Lua_SetFact, tableIndex, "SetFact");
            });
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_SetFact(IntPtr L)
        {
            string entityName = LuaArgumentReader.ReadString(L, 1);
            string factName = LuaArgumentReader.ReadString(L, 2);
            bool value = LuaArgumentReader.ReadBool(L, 3);

            BaseFacts facts = _entityRegistryLookup?.Invoke(entityName);
            facts?.Set<bool>(factName, value);
            return 0;
        }

        public void RunDirectorScript(string scriptCode)
        {
            _lua.ExecuteString(scriptCode, _zoneName);
        }

        public void Dispose()
        {
            _lua?.Dispose();
        }
    }
}