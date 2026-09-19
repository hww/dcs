using DCS.Core;
using DCS.Lua;
using System.Text;
using UnityEngine;

namespace DCS.Core
{
    /// <summary>
    /// Экранный инспектор хоста. Рисует список компонентов
    /// в верхнем левом углу с отступом 10px, жёлтым цветом.
    /// </summary>
    public class HostInspectorOverlay : MonoBehaviour
    {
        [Header("Settings")]
        public int FontSize = 14;
        public Color TextColor = Color.yellow;
        public float Margin = 10f;
        public float LineHeight = 18f;

        private GUIStyle _style;
        private readonly StringBuilder _sb = new StringBuilder(512);

        void OnGUI()
        {
            var reference = GetComponent<IHostReference>();
            if (reference == null) return;

            Host host = reference.Host;
            if (!HostManager.IsValid(host)) return;

            // Строим текст
            _sb.Clear();
            HostInspector.InspectHost(host, LuaManager._globalHostChain, _sb);

            // Стиль — один раз, чтобы не аллоцировать каждый кадр
            if (_style == null || _style.fontSize != FontSize)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = FontSize,
                    normal = { textColor = TextColor },
                    wordWrap = false,
                };
            }

            // Прямоугольник с margin от верхнего-левого угла
            var rect = new Rect(Margin, Margin, Screen.width - Margin * 2, 400);
            GUI.Label(rect, _sb.ToString(), _style);
        }
    }
}