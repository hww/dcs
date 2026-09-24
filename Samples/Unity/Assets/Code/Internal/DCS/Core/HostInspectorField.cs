using DCS.Core;
using DCS.Lua;
using UnityEngine;

namespace DCS.Core
{
    /// <summary>
    /// Рисует Gizmo с содержимым хоста.
    /// Требует, чтобы этот GameObject был связан с хостом через IHostReference.
    /// </summary>
    public class HostInspectorField : MonoBehaviour
    {
        [Header("Settings")]
        public EHostChain hostChain = 0;
        [TextArea(10,20)]
        public string info;

        private IHostReference _reference;

        void Awake()
        {
            _reference = GetComponent<IHostReference>();
        }

        void OnDrawGizmos()
        {
            if (_reference == null) _reference = GetComponent<IHostReference>();
            if (_reference == null) return;

            Host host = _reference.Host;
            if (!HostManager.IsValid(host)) return;

            var sb = new System.Text.StringBuilder();
            var chain = DomainRegistry.Get((int)hostChain).HostChain;
            HostInspector.InspectHost(host, chain, sb);
            info = sb.ToString();
        }
    }
}