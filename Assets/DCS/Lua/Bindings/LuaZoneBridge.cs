using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DynamicComponent.Lua
{ 
    public static class LuaZoneBridge
    {
        // Делегат, который мы передадим из нашего менеджера сцен, 
        // чтобы мост знал, в каком плоском списке искать объекты по имени.
        public static Func<string, BaseFacts> EntityLookup { get; set; }

        /// <summary>
        /// Биндинг для Lua-функции: Zone.SetFact(entityName, factName, value)
        /// </summary>
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        public static int Lua_SetFact(IntPtr L)
        {
            // Извлекаем аргументы из стека Lua по индексам (1, 2, 3)
            IntPtr entityNamePtr = LuaNative.lua_tolstring(L, 1, IntPtr.Zero);
            IntPtr factNamePtr = LuaNative.lua_tolstring(L, 2, IntPtr.Zero);
            
            // Проверяем тип третьего аргумента на стеке
            int isBool = LuaNative.lua_toboolean(L, 3);

            string entityName = Marshal.PtrToStringAnsi(entityNamePtr);
            string factName = Marshal.PtrToStringAnsi(factNamePtr);
            bool value = isBool != 0;

            // Ищем пассивный объект в плоском списке по его Длинному Имени
            BaseFacts facts = EntityLookup?.Invoke(entityName);
            if (facts != null)
            {
                // Записываем данные в DynamicFacts/BaseFacts объекта!
                // Unity-система (DOD) увидит это в следующем кадре.
                facts.Set<bool>(factName, value);
            }
            else
            {
                Debug.LogWarning($"[Lua Bridge] Сущность '{entityName}' не найдена в плоском списке зоны.");
            }

            return 0; // Функция ничего не возвращает в сам Lua-скрипт
        }

        /// <summary>
        /// Биндинг для Lua-функции: Zone.Log(message)
        /// </summary>
        [AOT.MonoPInvokeCallback(typeof(Func<IntPtr, int>))]
        public static int Lua_Log(IntPtr L)
        {
            IntPtr msgPtr = LuaNative.lua_tolstring(L, 1, IntPtr.Zero);
            string message = Marshal.PtrToStringAnsi(msgPtr);
            Debug.Log($"[Lua-Zone] {message}");
            return 0;
        }
    }
}
