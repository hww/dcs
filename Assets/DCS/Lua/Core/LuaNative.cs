using System;
using System.Runtime.InteropServices;

namespace DynamicComponent.Lua
{
    /// <summary>
    /// Low-level P/Invoke bindings for Lua 5.4 native library.
    /// </summary>
    public static class LuaNative
    {
        private const string LUA_DLL = "lua54";

        // ============================================================
        //  STATE MANAGEMENT
        // ============================================================

        [DllImport(LUA_DLL, EntryPoint = "luaL_newstate")]
        public static extern IntPtr luaL_newstate();

        [DllImport(LUA_DLL, EntryPoint = "luaL_openlibs")]
        public static extern void luaL_openlibs(IntPtr L);

        [DllImport(LUA_DLL, EntryPoint = "lua_close")]
        public static extern void lua_close(IntPtr L);

        // ============================================================
        //  LOADING & EXECUTION
        // ============================================================

        [DllImport(LUA_DLL, EntryPoint = "luaL_loadstring")]
        public static extern int luaL_loadstring(IntPtr L, string s);

        [DllImport(LUA_DLL, EntryPoint = "lua_pcallk")]
        public static extern int lua_pcallk(IntPtr L, int nargs, int nresults, int errfunc, int ctx, IntPtr k);

        [DllImport(LUA_DLL, EntryPoint = "lua_pcall")]
        public static extern int lua_pcall(IntPtr L, int nargs, int nresults, int errfunc);

        // ============================================================
        //  GLOBALS
        // ============================================================

        [DllImport(LUA_DLL, EntryPoint = "lua_setglobal")]
        public static extern void lua_setglobal(IntPtr L, string name);

        [DllImport(LUA_DLL, EntryPoint = "lua_getglobal")]
        public static extern int lua_getglobal(IntPtr L, string name);

        // ============================================================
        //  TABLES
        // ============================================================

        [DllImport(LUA_DLL, EntryPoint = "lua_createtable")]
        private static extern void lua_createtable(IntPtr L, int narr, int nrec);

        public static void lua_newtable(IntPtr L) => lua_createtable(L, 0, 0);

        [DllImport(LUA_DLL, EntryPoint = "lua_settable")]
        public static extern void lua_settable(IntPtr L, int idx);

        [DllImport(LUA_DLL, EntryPoint = "lua_gettable")]
        public static extern void lua_gettable(IntPtr L, int idx);

        [DllImport(LUA_DLL, EntryPoint = "lua_setfield")]
        public static extern void lua_setfield(IntPtr L, int idx, string k);

        [DllImport(LUA_DLL, EntryPoint = "lua_getfield")]
        public static extern void lua_getfield(IntPtr L, int idx, string k);

        // ============================================================
        //  STACK OPERATIONS
        // ============================================================

        [DllImport(LUA_DLL, EntryPoint = "lua_gettop")]
        public static extern int lua_gettop(IntPtr L);

        [DllImport(LUA_DLL, EntryPoint = "lua_settop")]
        public static extern void lua_settop(IntPtr L, int idx);

        [DllImport(LUA_DLL, EntryPoint = "lua_pushvalue")]
        public static extern void lua_pushvalue(IntPtr L, int idx);

        [DllImport(LUA_DLL, EntryPoint = "lua_remove")]
        public static extern void lua_remove(IntPtr L, int idx);

        [DllImport(LUA_DLL, EntryPoint = "lua_pop")]
        public static extern void lua_pop(IntPtr L, int n);

        // ============================================================
        //  PUSH VALUES
        // ============================================================

        [DllImport(LUA_DLL, EntryPoint = "lua_pushnil")]
        public static extern void lua_pushnil(IntPtr L);

        [DllImport(LUA_DLL, EntryPoint = "lua_pushboolean")]
        public static extern void lua_pushboolean(IntPtr L, int b);

        [DllImport(LUA_DLL, EntryPoint = "lua_pushinteger")]
        public static extern void lua_pushinteger(IntPtr L, long n);

        [DllImport(LUA_DLL, EntryPoint = "lua_pushnumber")]
        public static extern void lua_pushnumber(IntPtr L, double n);

        [DllImport(LUA_DLL, EntryPoint = "lua_pushstring")]
        public static extern IntPtr lua_pushstring(IntPtr L, string s);

        [DllImport(LUA_DLL, EntryPoint = "lua_pushcclosure")]
        public static extern void lua_pushcclosure(IntPtr L, IntPtr fn, int n);

        [DllImport(LUA_DLL, EntryPoint = "lua_pushlightuserdata")]
        public static extern void lua_pushlightuserdata(IntPtr L, IntPtr p);

        // ============================================================
        //  TO CONVERSION
        // ============================================================

        [DllImport(LUA_DLL, EntryPoint = "lua_tolstring")]
        public static extern IntPtr lua_tolstring(IntPtr L, int idx, IntPtr len);

        [DllImport(LUA_DLL, EntryPoint = "lua_toboolean")]
        public static extern int lua_toboolean(IntPtr L, int idx);

        [DllImport(LUA_DLL, EntryPoint = "lua_tointegerx")]
        public static extern long lua_tointegerx(IntPtr L, int idx, IntPtr isnum);

        [DllImport(LUA_DLL, EntryPoint = "lua_tonumberx")]
        public static extern double lua_tonumberx(IntPtr L, int idx, IntPtr isnum);

        [DllImport(LUA_DLL, EntryPoint = "lua_touserdata")]
        public static extern IntPtr lua_touserdata(IntPtr L, int idx);

        // ============================================================
        //  TYPE CHECKING
        // ============================================================

        [DllImport(LUA_DLL, EntryPoint = "lua_type")]
        public static extern int lua_type(IntPtr L, int idx);

        // ============================================================
        //  TYPE CONSTANTS
        // ============================================================

        public const int LUA_TNONE = -1;
        public const int LUA_TNIL = 0;
        public const int LUA_TBOOLEAN = 1;
        public const int LUA_TLIGHTUSERDATA = 2;
        public const int LUA_TNUMBER = 3;
        public const int LUA_TSTRING = 4;
        public const int LUA_TTABLE = 5;
        public const int LUA_TFUNCTION = 6;
        public const int LUA_TUSERDATA = 7;
        public const int LUA_TTHREAD = 8;

        // ============================================================
        //  ERROR HANDLING
        // ============================================================

        [DllImport(LUA_DLL, EntryPoint = "luaL_error")]
        public static extern int luaL_error(IntPtr L, string fmt);

        // ============================================================
        //  GC
        // ============================================================

        [DllImport(LUA_DLL, EntryPoint = "lua_gc")]
        public static extern int lua_gc(IntPtr L, int what, int data);

        public const int LUA_GCSTOP = 0;
        public const int LUA_GCRESTART = 1;
        public const int LUA_GCCOLLECT = 2;
        public const int LUA_GCCOUNT = 3;
        public const int LUA_GCCOUNTB = 4;
        public const int LUA_GCSTEP = 5;
        public const int LUA_GCSETPAUSE = 6;
        public const int LUA_GCSETSTEPMUL = 7;
    }
}