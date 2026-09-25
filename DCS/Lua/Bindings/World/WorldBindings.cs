using System;

namespace DCS.Lua.Bindings
{
    public static class WorldBindings
    {
        public static void Register(IntPtr L)
        {
            LuaBindings.RegisterNamespace(L, "World", (state, tableIndex) =>
            {
                ActorBindings.Register(state, tableIndex);
                ActorRegistrationBindings.Register(state, tableIndex);
            });
            

        }
    }
}