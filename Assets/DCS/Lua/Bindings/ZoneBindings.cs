using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DynamicComponent.Lua.Bindings
{
    public static class ZoneBindings
    {
        // Делегат-шлюз, который прокидывает рантайм мира (World), 
        // чтобы скрипты зон могли изменять факты сущностей в пулах симуляции
        public static Func<string, BaseFacts> EntityLookup { get; set; }

        public static void Register(IntPtr L)
        {
            // Создаем изолированное пространство имен (таблицу) Zone в Lua-стейте
            LuaNative.lua_newtable(L);

            RegisterMethod(L, "SetFact", Lua_SetFact);
            RegisterMethod(L, "Log", Lua_Log);

            LuaNative.lua_setglobal(L, "Zone");
        }

        private static void RegisterMethod(IntPtr L, string name, Func<IntPtr, int> fn)
        {
            LuaNative.lua_pushstring(L, name);
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(fn);
            LuaNative.lua_pushcclosure(L, ptr, 0);
            LuaNative.lua_settable(L, -3);
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_SetFact(IntPtr L)
        {
            IntPtr entityNamePtr = LuaNative.lua_tolstring(L, 1, IntPtr.Zero);
            IntPtr factNamePtr = LuaNative.lua_tolstring(L, 2, IntPtr.Zero);
            int isBool = LuaNative.lua_toboolean(L, 3);

            string entityName = Marshal.PtrToStringUTF8(entityNamePtr); // Перешли на UTF8 для надежности
            string factName = Marshal.PtrToStringUTF8(factNamePtr);
            bool value = isBool != 0;

            BaseFacts facts = EntityLookup?.Invoke(entityName);
            if (facts != null)
            {
                // Записываем данные в DynamicFacts объекта, симуляция увидит это в следующем кадре
                facts.Set<bool>(factName, value);
            }
            else
            {
                Debug.LogWarning($"[Zone Lua API] Сущность '{entityName}' не найдена в плоском реестре активного датасета.");
            }

            return 0;
        }

        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        private static int Lua_Log(IntPtr L)
        {
            IntPtr msgPtr = LuaNative.lua_tolstring(L, 1, IntPtr.Zero);
            string message = Marshal.PtrToStringUTF8(msgPtr);
            Debug.Log($"<color=olive>[Lua-Zone Direct]</color> {message}");
            return 0;
        }
    }
}
