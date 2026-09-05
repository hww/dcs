using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class LuaTest : MonoBehaviour
{
    // Биндим 5 базовых функций напрямую из файла lua54.dll, который лежит у вас в проекте
    [DllImport("lua54", EntryPoint = "luaL_newstate")]
    private static extern IntPtr luaL_newstate();

    [DllImport("lua54", EntryPoint = "luaL_openlibs")]
    private static extern void luaL_openlibs(IntPtr L);

    [DllImport("lua54", EntryPoint = "luaL_loadstring")]
    private static extern int luaL_loadstring(IntPtr L, string s);

    [DllImport("lua54", EntryPoint = "lua_pcallk")]
    private static extern int lua_pcallk(IntPtr L, int nargs, int nresults, int errfunc, int ctx, IntPtr k);

    [DllImport("lua54", EntryPoint = "lua_close")]
    private static extern void lua_close(IntPtr L);

    private IntPtr L;

    void OnEnable()
    {
        // 1. Создаем чистую виртуальную машину Lua
        L = luaL_newstate();
        if (L == IntPtr.Zero)
        {
            Debug.LogError("[Lua] Ошибка: Не удалось запустить интерпретатор.");
            return;
        }

        // 2. Открываем базовые библиотеки Lua (math, table, string)
        luaL_openlibs(L);

        // 3. Пишем скрипт на чистом Lua 5.4
        string luaCode = "print('УРА! Чистая система Lua 5.4 успешно работает в Unity!')";

        // 4. Загружаем и выполняем этот скрипт
        if (luaL_loadstring(L, luaCode) == 0)
        {
            lua_pcallk(L, 0, -1, 0, 0, IntPtr.Zero);
            Debug.Log("[Lua] Выполнение скрипта полностью завершено.");
        }
        else
        {
            Debug.LogError("[Lua] Ошибка синтаксиса в коде Lua.");
        }
    }

    void OnDisable()
    { 
        // 5. Обязательно закрываем сессию и очищаем память
        lua_close(L);
    }

}
