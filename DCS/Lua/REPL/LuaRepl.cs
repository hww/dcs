using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace DCS.Lua
{
    public class LuaRepl
    {
        private readonly IntPtr _L;
        private const int LUA_OK = 0;

        public LuaRepl(IntPtr L)
        {
            if (L == IntPtr.Zero) throw new ArgumentException("lua_State is null");
            _L = L;
        }

        /// <summary>
        /// Вычислить выражение/код. Вызывается СТРОГО в главном потоке Unity.
        /// </summary>
        public string Eval(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            // Намертво фиксируем размер стека до начала выполнения пакета
            int startTop = LuaNative.lua_gettop(_L);
            var stdoutAccumulator = new StringBuilder();

            // Включаем редирект для print()
            LuaStateWrapper.ActiveLogRedirect = (msg) => stdoutAccumulator.AppendLine(msg);

            try
            {
                int topBeforeRun = LuaNative.lua_gettop(_L);

                // 1. Пробуем скомпилировать как выражение (с return)
                int status = LuaNative.luaL_loadstring(_L, "return " + input);
                if (status != LUA_OK)
                {
                    // Ошибка синтаксиса выражения — срезаем стек строго до зоны безопасности
                    LuaNative.lua_settop(_L, topBeforeRun);

                    // Пробуем скомпилировать как обычный блок кода/стейтмент
                    status = LuaNative.luaL_loadstring(_L, input);
                }

                if (status != LUA_OK)
                {
                    // БРОНИРОВАННЫЙ разбор ошибок синтаксиса: забираем сырой указатель без вызова нативного тостринга
                    int errIdx = LuaNative.lua_gettop(_L);
                    IntPtr ptr = LuaNative.lua_tolstring(_L, errIdx, IntPtr.Zero);
                    string err = (ptr != IntPtr.Zero) ? Marshal.PtrToStringAnsi(ptr) : "unknown syntax error";
                    return $"SYNTAX ERROR: {err}\n";
                }

                // 2. Выполняем чанк через защищенный pcall
                status = LuaNative.lua_pcallk(_L, 0, -1, 0, 0, IntPtr.Zero);
                if (status != LUA_OK)
                {
                    // БРОНИРОВАННЫЙ разбор рантайм-ошибок (например, класса не существует)
                    int errIdx = LuaNative.lua_gettop(_L);
                    IntPtr ptr = LuaNative.lua_tolstring(_L, errIdx, IntPtr.Zero);
                    string err = (ptr != IntPtr.Zero) ? Marshal.PtrToStringAnsi(ptr) : "unknown runtime error";
                    return $"RUNTIME ERROR: {err}\n";
                }

                // 3. Считаем валидные return-значения
                int currentTop = LuaNative.lua_gettop(_L);
                int nres = currentTop - topBeforeRun;

                var resultBuilder = new StringBuilder();

                // Вытаскиваем то, что успел наловить print
                if (stdoutAccumulator.Length > 0)
                {
                    resultBuilder.Append(stdoutAccumulator.ToString());
                }

                // Форматируем return-значения
                for (int i = 1; i <= nres; i++)
                {
                    if (i > 1) resultBuilder.Append('\t');
                    resultBuilder.Append(FormatValueSafe(_L, topBeforeRun + i));
                }

                if (nres > 0) resultBuilder.Append('\n');

                if (resultBuilder.Length == 0) resultBuilder.Append("ok\n");

                return resultBuilder.ToString();
            }
            catch (Exception ex)
            {
                return $"INTERNAL REPL EXCEPTION: {ex.Message}\n";
            }
            finally
            {
                // Выключаем редирект принтов
                LuaStateWrapper.ActiveLogRedirect = null;

                // ЖЕЛЕЗНЫЙ ЗАКОН: Возвращаем стек к исходному размеру, полностью стирая любой мусор
                LuaNative.lua_settop(_L, startTop);
            }
        }

        private string FormatValueSafe(IntPtr L, int idx)
        {
            int t = LuaNative.lua_type(L, idx);
            switch (t)
            {
                case LuaNative.LUA_TNIL: return "nil";
                case LuaNative.LUA_TBOOLEAN: return LuaNative.lua_toboolean(L, idx) != 0 ? "true" : "false";
                case LuaNative.LUA_TNUMBER:
                    {
                        double d = LuaNative.lua_tonumberx(L, idx, IntPtr.Zero);
                        long i = LuaNative.lua_tointegerx(L, idx, IntPtr.Zero);
                        return (d == i) ? i.ToString() : d.ToString("G");
                    }
                case LuaNative.LUA_TSTRING:
                    {
                        IntPtr ptr = LuaNative.lua_tolstring(L, idx, IntPtr.Zero);
                        return (ptr != IntPtr.Zero) ? Marshal.PtrToStringAnsi(ptr) : "";
                    }
                case LuaNative.LUA_TTABLE: return "table";
                case LuaNative.LUA_TFUNCTION: return "function";
                case LuaNative.LUA_TUSERDATA: return "userdata";
                default: return $"<type {t}>";
            }
        }
    }
}
