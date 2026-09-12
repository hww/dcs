using DynamicComponent;
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
        DynamicComponent.Lua.LuaManager.BindHostChain(myGameChain);
        DynamicComponent.Lua.LuaManager.BindEventSystems(mySubPool, myTypeChain);
    }
}
