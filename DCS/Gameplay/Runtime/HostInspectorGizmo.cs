using DCS.Core;
using DCS.Lua;
using UnityEngine;

namespace DCS.Core
{
    /// <summary>
    /// Рисует Gizmo с содержимым хоста.
    /// Требует, чтобы этот GameObject был связан с хостом через IHostReference.
    /// </summary>
    public class HostInspectorGizmo : MonoBehaviour
    {
        [Header("Settings")]
        public float OffsetY = 2.5f;
        public int FontSize = 12;
        public Color TextColor = Color.white;
        public bool AlwaysVisible = false;
        public float MaxDistance = 30f;

        private IHostReference _reference;

        void Awake()
        {
            _reference = GetComponent<IHostReference>();
        }

        void OnDrawGizmos()
        {
            if (!AlwaysVisible && !Application.isPlaying) return;
            if (_reference == null) _reference = GetComponent<IHostReference>();
            if (_reference == null) return;

            Host host = _reference.Host;
            if (!HostManager.IsValid(host)) return;

            if (UnityEngine.Camera.current != null && !AlwaysVisible)
            {
                float d = Vector3.Distance(UnityEngine.Camera.current.transform.position, transform.position);
                if (d > MaxDistance) return;
            }

            var sb = new System.Text.StringBuilder();
            HostInspector.InspectHost(host, LuaManager._globalHostChain, sb);
            string text = sb.ToString();

#if UNITY_EDITOR
            UnityEditor.Handles.color = TextColor;
            UnityEditor.Handles.Label(transform.position + Vector3.up * OffsetY, text);
#endif
        }
    }
}