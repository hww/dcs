using DynamicComponent.Lua;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DynamicComponent
{
    [Serializable]
    public class DynamicFacts : BaseFacts, ISerializationCallbackReceiver
    {
        [SerializeField, TextArea(3, 20)]
        private string jsonData = "{}";

        private Dictionary<string, string> facts = new Dictionary<string, string>();

        // ==================== PUBLIC API ====================

        public string ToJson()
        {
            return JsonParser.ConvertToJsonString(facts);
        }

        // ==================== SERIALIZATION ====================

        public void OnBeforeSerialize()
        {
            jsonData = ToJson();
        }

        public void OnAfterDeserialize()
        {
            facts = JsonParser.ParseJsonStringToDictionary(jsonData);
        }

        // ==================== VALUE CONVERSION ====================

        protected override bool TryGetInternal<T>(string name, out T value)
        {
            value = default;

            if (facts.TryGetValue(name, out string jsonValue) && !string.IsNullOrEmpty(jsonValue))
            {
                try
                {
                    value = JsonParser.ConvertFromJson<T>(jsonValue);
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Failed to convert fact " + name + ": " + e.Message);
                }
            }

            return false;
        }

        protected override void SetInternal<T>(string name, T value)
        {
            facts[name] = JsonParser.ConvertToJson(value);
        }

        // ==================== REMAINING METHODS ====================

        protected override bool RemoveInternal(string name)
        {
            return facts.Remove(name);
        }

        protected override bool ContainsInternal(string name)
        {
            return facts.ContainsKey(name);
        }

        protected override void ClearInternal()
        {
            facts.Clear();
        }

        public override string ToString()
        {
            return ToJson();
        }

        // ============================================================
        //  LUA API: READ INTERFACE (GET)
        // ============================================================
        public override bool GetFact(string fieldName, IntPtr L)
        {
            if (!facts.TryGetValue(fieldName, out string jsonValue) || string.IsNullOrEmpty(jsonValue))
            {
                return false;
            }

            string trimmed = jsonValue.Trim();

            if (trimmed == "null")
            {
                LuaNative.lua_pushnil(L);
                return true;
            }
            if (trimmed == "true" || trimmed == "false")
            {
                LuaNative.lua_pushboolean(L, trimmed == "true");
                return true;
            }
            if (trimmed.StartsWith("\"") && trimmed.EndsWith("\""))
            {
                string rawStr = trimmed.Substring(1, trimmed.Length - 2);
                LuaNative.lua_pushstring(L, rawStr);
                return true;
            }
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                return ParseAndPushArrayToLua(trimmed, L);
            }

            if (double.TryParse(trimmed, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double num))
            {
                LuaNative.lua_pushnumber(L, num);
                return true;
            }

            Debug.LogWarning("[DCS Facts] Failed to push fact " + fieldName + " to Lua. Unknown numeric format: " + trimmed);
            return false;
        }

        private bool ParseAndPushArrayToLua(string jsonArray, IntPtr L)
        {
            var clean = jsonArray.Trim('[', ']');
            var parts = clean.Split(',');
            var error = false;

            foreach (var part in parts)
            {
                if (double.TryParse(part.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
                {
                    LuaNative.lua_pushnumber(L, val);
                }
                else
                {
                    LuaNative.lua_pushnumber(L, 0);
                    error = true;
                }
            }

            return !error;
        }

        // ============================================================
        //  LUA API: WRITE INTERFACE (SET)
        // ============================================================
        public override unsafe bool SetFact(string fieldName, IntPtr L)
        {
            int luaType = LuaNative.lua_type(L, -1);

            switch (luaType)
            {
                case 0: // NIL
                    facts[fieldName] = "null";
                    return true;

                case 1: // BOOLEAN
                    bool b = LuaNative.lua_toboolean(L, -1) != 0;
                    facts[fieldName] = b ? "true" : "false";
                    return true;

                case 3: // NUMBER
                    int isNum;
                    double num = LuaNative.lua_tonumberx(L, -1, (IntPtr)(&isNum));
                    if (isNum != 0)
                    {
                        facts[fieldName] = num.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        return true;
                    }
                    return false;

                case 4: // STRING
                    IntPtr strPtr = LuaNative.lua_tolstring(L, -1, IntPtr.Zero);
                    if (strPtr != IntPtr.Zero)
                    {
                        string s = System.Runtime.InteropServices.Marshal.PtrToStringUTF8(strPtr);
                        facts[fieldName] = "\"" + JsonParser.EscapeJsonString(s) + "\"";
                        return true;
                    }
                    return false;

                default:
                    Debug.LogWarning("[DCS Facts] SetField for type " + luaType + " is not supported directly from stack yet.");
                    return false;
            }
        }
    }
}
