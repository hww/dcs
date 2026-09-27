using System;

namespace DCS.Core
{
    /// <summary>
    /// Единая конфигурация лимитов DCS.
    /// Инвариант: MaxComponentsPerHost * MaxGameObjects <= MaxChainNodes.
    /// </summary>
    public static class DcsConfig
    {
        public const int MaxGameObjects = 65535;
        public const int MaxComponentsPerHost = 8;    // с запасом на будущее
        public const int MaxChainNodes = MaxGameObjects * MaxComponentsPerHost;
        public const int MaxTypeNodes = 100000;
        public const int MaxComponentTypes = 200;
    }
}