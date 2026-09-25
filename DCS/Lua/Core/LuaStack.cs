using DCS.Core;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DCS.Lua
{
    /// <summary>
    /// Validated Lua stack readers used by actor field accessors.
    /// On failure, logs an error with field name and host context.
    /// </summary>
    public static class LuaStack
    {
        public static unsafe bool TryGetInt(
            IntPtr L, int idx, string fieldName, Host host, out int result)
        {
            int isNum;
            long val = LuaNative.lua_tointegerx(L, idx, (IntPtr)(&isNum));
            if (isNum != 0)
            {
                result = (int)val;
                return true;
            }
            result = 0;
            Debug.LogError(
                $"[DCS Error] Field '{fieldName}' expected Integer " +
                $"on Host {host.Id} (Gen {host.Generation})");
            return false;
        }

        public static unsafe bool TryGetFloat(
            IntPtr L, int idx, string fieldName, Host host, out float result)
        {
            int isNum;
            double val = LuaNative.lua_tonumberx(L, idx, (IntPtr)(&isNum));
            if (isNum != 0)
            {
                result = (float)val;
                return true;
            }
            result = 0f;
            Debug.LogError(
                $"[DCS Error] Field '{fieldName}' expected Float " +
                $"on Host {host.Id} (Gen {host.Generation})");
            return false;
        }

        public static bool TryGetString(
            IntPtr L, int idx, string fieldName, Host host, out string result)
        {
            IntPtr ptr = LuaNative.lua_tolstring(L, idx, IntPtr.Zero);
            if (ptr != IntPtr.Zero)
            {
                result = Marshal.PtrToStringUTF8(ptr);
                return true;
            }
            result = null;
            Debug.LogError(
                $"[DCS Error] Field '{fieldName}' expected String " +
                $"on Host {host.Id} (Gen {host.Generation})");
            return false;
        }

        public static unsafe bool TryGetVector3(
            IntPtr L, int startIdx, string fieldName, Host host, out Vector3 vector)
        {
            int isX, isY, isZ;
            double z = LuaNative.lua_tonumberx(L, startIdx, (IntPtr)(&isZ));
            double y = LuaNative.lua_tonumberx(L, startIdx - 1, (IntPtr)(&isY));
            double x = LuaNative.lua_tonumberx(L, startIdx - 2, (IntPtr)(&isX));

            if (isX != 0 && isY != 0 && isZ != 0)
            {
                vector = new Vector3((float)x, (float)y, (float)z);
                return true;
            }
            vector = Vector3.zero;
            Debug.LogError(
                $"[DCS Error] Field '{fieldName}' expected Vector3 " +
                $"on Host {host.Id} (Gen {host.Generation})");
            return false;
        }
    }
}