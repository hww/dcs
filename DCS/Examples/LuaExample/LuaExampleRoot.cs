using DCS.Lua;
using UnityEngine;

namespace DCS.Examples.LuaExample
{
    /// <summary>
    /// Минимальный пример использования LuaManager.
    /// Показывает: как вызвать Lua-функцию из C#.
    /// </summary>
    public class LuaExampleRoot : MonoBehaviour
    {
        void OnEnable()
        {
            // Один раз: вызов Lua boot-функции
            LuaManager.Instance.CallGlobal("DCS_Global_GameBoot");
        }

        void Update()
        {
            // Каждый кадр: тик Lua-процессов
            LuaManager.Instance.CallGlobal("DCS_Global_FrameUpdate");
        }
    }
}