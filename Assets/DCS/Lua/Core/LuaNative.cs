using System;
using System.Runtime.InteropServices;

namespace DynamicComponent.Lua
{
    public static class LuaNative
    {
        private const string LUA_DLL = "lua54";

        // Ваши исходные 5 функций
        [DllImport(LUA_DLL, EntryPoint = "luaL_newstate")]
        public static extern IntPtr luaL_newstate();

        [DllImport(LUA_DLL, EntryPoint = "luaL_openlibs")]
        public static extern void luaL_openlibs(IntPtr L);

        [DllImport(LUA_DLL, EntryPoint = "luaL_loadstring")]
        public static extern int luaL_loadstring(IntPtr L, string s);

        [DllImport(LUA_DLL, EntryPoint = "lua_pcallk")]
        public static extern int lua_pcallk(IntPtr L, int nargs, int nresults, int errfunc, int ctx, IntPtr k);

        [DllImport(LUA_DLL, EntryPoint = "lua_close")]
        public static extern void lua_close(IntPtr L);

        [DllImport(LUA_DLL, EntryPoint = "lua_tolstring")]
        public static extern IntPtr lua_tolstring(IntPtr L, int idx, IntPtr len);

        [DllImport(LUA_DLL, EntryPoint = "lua_toboolean")]
        public static extern int lua_toboolean(IntPtr L, int idx);

        [DllImport(LUA_DLL, EntryPoint = "lua_pushcclosure")]
        public static extern void lua_pushcclosure(IntPtr L, IntPtr fn, int n);

        [DllImport(LUA_DLL, EntryPoint = "lua_setglobal")]
        public static extern void lua_setglobal(IntPtr L, string name);

        // --- ИСПРАВЛЕНИЕ: Точные импорты для работы с таблицами и строками, которых не хватало ---

        // Для lua_newtable (в Си это макрос над lua_createtable)
        [DllImport(LUA_DLL, EntryPoint = "lua_createtable")]
        private static extern void lua_createtable(IntPtr L, int narr, int nrec);

        public static void lua_newtable(IntPtr L) => lua_createtable(L, 0, 0);

        // Для lua_pushstring
        [DllImport(LUA_DLL, EntryPoint = "lua_pushstring")]
        public static extern IntPtr lua_pushstring(IntPtr L, string s);

        // Для lua_settable
        [DllImport(LUA_DLL, EntryPoint = "lua_settable")]
        public static extern void lua_settable(IntPtr L, int idx);

        [DllImport(LUA_DLL, EntryPoint = "lua_pushinteger")]
        public static extern void lua_pushinteger(IntPtr L, long n);

        [DllImport(LUA_DLL, EntryPoint = "lua_tointegerx")]
        public static extern long lua_tointegerx(IntPtr L, int idx, IntPtr isnum);

    }
}
