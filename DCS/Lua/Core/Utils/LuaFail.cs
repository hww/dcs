using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DCS.Lua
{
    internal static class LuaFail
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [DebuggerHidden]
        public static void Fail(IntPtr L, string funcName, string reason)
        {
            string msg = string.IsNullOrEmpty(funcName)
                ? reason
                : $"argument error in '{funcName}': {reason}";

            string safe = msg.IndexOf('%') >= 0
                ? msg.Replace("%", "%%")
                : msg;

            LuaNative.luaL_error(L, safe);
            throw new LuaErrorUnreachableException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [DebuggerHidden]
        public static void Error(IntPtr L, string msg)
        {
            string safe = msg.IndexOf('%') >= 0
                ? msg.Replace("%", "%%")
                : msg;

            LuaNative.luaL_error(L, safe);
            throw new LuaErrorUnreachableException();
        }
    }

    internal sealed class LuaErrorUnreachableException : Exception
    {
        public LuaErrorUnreachableException()
            : base("luaL_error returned — longjmp did not unwind. " +
                   "Check P/Invoke signature.")
        { }
    }
}