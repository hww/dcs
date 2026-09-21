using DCS.Core;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public static Domain _domain;


    public static void BindDomain(Domain domain)
    {
        _domain = domain;
    }


    void Awake()
    {
        // 1. Initialize your high-performance DCS pools
        ComponentRegistry.InitializeAllPools();

        // 2. Create your core runtime chain managers
        HostChain myGameChain = DomainRegistry.Create("Default").HostChain;
        TypeChain myTypeChain = new TypeChain();
        EventSubscription mySubPool = new EventSubscription(1000);

    }
}
