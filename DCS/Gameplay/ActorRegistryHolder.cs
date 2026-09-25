using DCS.Spatial;  // если нужен

namespace DCS.Core
{
    public static class ActorRegistryHolder
    {
        public static ActorRegistry Instance { get; private set; }
        public static void Set(ActorRegistry registry) => Instance = registry;
        public static ActorRegistry EnsureCreated()
        {
            if (Instance == null)
                Instance = new ActorRegistry();
            return Instance;
        }
    }
}