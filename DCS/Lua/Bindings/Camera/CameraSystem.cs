namespace DCS.Core
{
    /// <summary>
    /// Поиск камер по имени.
    /// Знает только CameraNameComponent. Без рефлексии, без Lua, без typeId снаружи.
    /// </summary>
    public static class CameraSystem
    {
        public const string MainCameraName = "MainCamera";

        public static bool TryFindByName(string expectedName,
                                         HostChain chain,
                                         out Host result)
        {
            result = default;
            if (string.IsNullOrEmpty(expectedName)) return false;
            if (chain == null) return false;

            var pool = ComponentRegistry.GetPool<CameraNameComponent>();
            if (pool == null) return false;

            int typeId = ComponentType<CameraNameComponent>.Id;
            int count = HostManager.GlobalHosts.Length;

            for (int i = 0; i < count; i++)
            {
                Host host = new Host
                {
                    Id = (ushort)i,
                    Generation = HostManager.GlobalHosts[i].Generation
                };
                if (!HostManager.IsValid(host)) continue;

                ChainNode node = chain.GetTypedHandle(host, typeId);
                if (node.IsNull) continue;

                int denseIndex;
                if (!pool.TryGetDenseIndex(node.Component, out denseIndex)) continue;

                ref CameraNameComponent cam = ref pool.Components[denseIndex];
                if (cam.Name != expectedName) continue;

                result = host;
                return true;
            }

            return false;
        }

        public static bool TryFindMain(HostChain chain, out Host result)
        {
            return TryFindByName(MainCameraName, chain, out result);
        }
    }
}