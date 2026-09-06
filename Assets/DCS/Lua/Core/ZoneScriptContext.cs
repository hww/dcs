using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DynamicComponent.Lua
{
    public class ZoneScriptContext : IDisposable
    {
        private readonly LuaStateWrapper _lua;
        private readonly string _zoneName;
        private readonly Func<string, BaseFacts> _entityRegistryLookup;

        public ZoneScriptContext(string zoneName, Func<string, BaseFacts> registryLookup)
        {
            _zoneName = zoneName;
            _entityRegistryLookup = registryLookup;
            _lua = new LuaStateWrapper(zoneName);

            RegisterZoneAPI();
        }

        private void RegisterZoneAPI()
        {
            IntPtr L = _lua.L;

            // Create local zone isolated table namespace
            LuaNative.lua_newtable(L);

            LuaNative.lua_pushstring(L, "SetFact");
            IntPtr setFactPtr = Marshal.GetFunctionPointerForDelegate((Func<IntPtr, int>)Lua_SetFact);
            LuaNative.lua_pushcclosure(L, setFactPtr, 0);
            LuaNative.lua_settable(L, -3);

            LuaNative.lua_setglobal(L, "Zone");
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private int Lua_SetFact(IntPtr L)
        {
            string entityName = Marshal.PtrToStringAnsi(LuaNative.lua_tolstring(L, 1, IntPtr.Zero));
            string factName = Marshal.PtrToStringAnsi(LuaNative.lua_tolstring(L, 2, IntPtr.Zero));
            bool value = LuaNative.lua_toboolean(L, 3) != 0;

            BaseFacts facts = _entityRegistryLookup?.Invoke(entityName);
            if (facts != null)
            {
                facts.Set<bool>(factName, value);
            }
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
