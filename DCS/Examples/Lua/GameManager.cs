using DCS.Core;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    void Awake()
    {
        // 1. Initialize your high-performance DCS pools
        ComponentRegistry.InitializeAllPools();

        // 2. Create your core runtime chain managers
        HostChain myGameChain = new HostChain();
        TypeChain myTypeChain = new TypeChain();
        EventSubscription mySubPool = new EventSubscription(1000);

        // 3. LINK BOTH DATA AND EVENT CHANNELS STRAIGHT TO LUA INFRASTRUCTURE
        DCS.Lua.LuaManager.BindHostChain(myGameChain);
        DCS.Lua.LuaManager.BindEventSystems(mySubPool, myTypeChain);
    }
}
