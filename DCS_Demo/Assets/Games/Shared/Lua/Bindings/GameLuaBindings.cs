using DCS.Lua;
using System;
using System.Runtime.InteropServices;

namespace DCS.Game
{
    public static class GameLuaBindings
    {
        public static void RegisterAll(IntPtr L)
        {
            SoldierBindings.Register(L);
        }

    }
}