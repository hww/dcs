using UnityEngine;

namespace DCS.Lua
{
    /// <summary>
    /// Параметры инициализации LuaManager.
    /// Передаётся из Root игры.
    /// </summary>
    public struct LuaManagerConfig
    {
        /// <summary>Корневая папка Lua-скриптов (без слеша в конце).</summary>
        public string LuaRootPath;

        /// <summary>Включать ли TCP-REPL для отладки.</summary>
        public bool EnableRepl;

        /// <summary>Порт REPL.</summary>
        public int ReplPort;

        public static LuaManagerConfig Default => new LuaManagerConfig
        {
            LuaRootPath = System.IO.Path.Combine(
                Application.streamingAssetsPath, "Lua").Replace("\\", "/"),
            EnableRepl = true,
            ReplPort = 49155
        };
    }
}