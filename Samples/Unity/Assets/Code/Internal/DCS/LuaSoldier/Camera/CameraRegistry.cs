using System.Collections.Generic;

namespace DCS.Core
{
    /// <summary>
    /// Системный реестр камер: имя → Host.
    /// Не знает про MonoBehaviours, мышь, ThirdPersonCamera.
    /// Просто маппинг "камера с именем X" ↔ Host.
    /// </summary>
    public static class CameraRegistry
    {
        private static readonly Dictionary<string, Host> _byName = new();
        private static Host _mainCameraHost = default;

        public static void RegisterCamera(string name, Host host, bool isMain = false)
        {
            if (string.IsNullOrEmpty(name)) return;
            _byName[name] = host;
            if (isMain) _mainCameraHost = host;
        }

        public static void UnregisterCamera(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            _byName.Remove(name);
            if (_byName.Count == 0) _mainCameraHost = default;
        }

        public static Host GetCameraHost(string name)
        {
            if (string.IsNullOrEmpty(name)) return default;
            return _byName.TryGetValue(name, out Host h) ? h : default;
        }

        public static Host GetMainCameraHost() => _mainCameraHost;

        public static void Clear()
        {
            _byName.Clear();
            _mainCameraHost = default;
        }
    }
}