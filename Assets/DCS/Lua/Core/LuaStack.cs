using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DynamicComponent.Lua
{
    public static class LuaStack
    {
        // ============================================================
        //  МЯГКИЙ API: С ИСПОЛЬЗОВАНИЕМ ДЕФОЛТНЫХ ЗНАЧЕНИЙ (OrDefault)
        // ============================================================

        public static unsafe int GetIntOrDefault(IntPtr L, int idx, int defaultValue = 0)
        {
            int isNum;
            long val = LuaNative.lua_tointegerx(L, idx, (IntPtr)(&isNum));
            return isNum != 0 ? (int)val : defaultValue;
        }

        public static unsafe float GetFloatOrDefault(IntPtr L, int idx, float defaultValue = 0f)
        {
            int isNum;
            double val = LuaNative.lua_tonumberx(L, idx, (IntPtr)(&isNum));
            return isNum != 0 ? (float)val : defaultValue;
        }

        public static string GetStringOrDefault(IntPtr L, int idx, string defaultValue = "")
        {
            IntPtr strPtr = LuaNative.lua_tolstring(L, idx, IntPtr.Zero);
            return strPtr != IntPtr.Zero ? Marshal.PtrToStringUTF8(strPtr) : defaultValue;
        }

        // ============================================================
        //  ЖЕСТКИЙ API: С АВТОМАТИЧЕСКОЙ ГЕНЕРАЦИЕЙ ОШИБОК (TryGet)
        // ============================================================

        public static unsafe bool TryGetInt(IntPtr L, int idx, string fieldName, Host host, out int result)
        {
            int isNum;
            long val = LuaNative.lua_tointegerx(L, idx, (IntPtr)(&isNum));
            if (isNum != 0)
            {
                result = (int)val;
                return true;
            }

            result = 0;
            Debug.LogError($"[DCS Error] Field '{fieldName}' expected an Integer on Host ID: {host.Id} (Gen: {host.Generation})");
            return false;
        }

        public static unsafe bool TryGetFloat(IntPtr L, int idx, string fieldName, Host host, out float result)
        {
            int isNum;
            double val = LuaNative.lua_tonumberx(L, idx, (IntPtr)(&isNum));
            if (isNum != 0)
            {
                result = (float)val;
                return true;
            }

            result = 0f;
            Debug.LogError($"[DCS Error] Field '{fieldName}' expected a Float/Number on Host ID: {host.Id} (Gen: {host.Generation})");
            return false;
        }

        public static bool TryGetString(IntPtr L, int idx, string fieldName, Host host, out string result)
        {
            IntPtr strPtr = LuaNative.lua_tolstring(L, idx, IntPtr.Zero);
            if (strPtr != IntPtr.Zero)
            {
                result = Marshal.PtrToStringUTF8(strPtr);
                return true;
            }

            result = null;
            Debug.LogError($"[DCS Error] Field '{fieldName}' expected a String on Host ID: {host.Id} (Gen: {host.Generation})");
            return false;
        }

        public static unsafe bool TryGetVector3(IntPtr L, int startIdx, string fieldName, Host host, out Vector3 vector)
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
            Debug.LogError($"[DCS Error] Field '{fieldName}' expected a Vector3 (3 numbers) on Host ID: {host.Id}");
            return false;
        }
    }
}
