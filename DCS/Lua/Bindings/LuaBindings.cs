using System;

namespace DCS.Lua.Bindings
{
    /// <summary>
    /// Single registration entry point for the current binding set.
    /// </summary>
    public static class LuaBindings
    {
        public static void RegisterAll(IntPtr L)
        {
            DcsBindings.Register(L);
            DynamicFactsBinding.Register(L);
            EventBindings.Register(L);
            SpatialBindings.Register(L);
            WorldBindings.Register(L);
            MapBindings.Register(L);
        }
    }
}
