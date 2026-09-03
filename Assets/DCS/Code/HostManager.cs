using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

// Игровой объект (абсолютно невесом)
public struct DCHost 
{
    public int Id;
    public int Generation;
    public int FirstComponent;
    public int Next;
}


public struct HostHandle 
{
    public int Id;
    public int Generation;
    public bool IsNull => Generation == 0; 

}


public static class HostManager
{
    public const int MaxGameObjects = 100000;
    // Массив хостов остается здесь как в Варианте А
    public static readonly DCHost[] GlobalHosts = new DCHost[MaxGameObjects];
    private static int _firstFree = 0;

    static HostManager()
    {
        for (int i = 0; i < MaxGameObjects; i++)
        {
            GlobalHosts[i] = new DCHost { Id = i, Generation = 1, FirstComponent = -1, Next = i + 1 };
        }
        GlobalHosts[MaxGameObjects-1].Next = -1; 
        _firstFree = 0;
    }

    public static HostHandle CreateHost()
    {
        if (_firstFree < 0) 
            throw new System.Exception("DCS Error: Превышен лимит HostID!");
        
        int id = _firstFree;
        GlobalHosts[id].Generation++;
        _firstFree = GlobalHosts[id].Next;
        return new HostHandle { Id = id, Generation =  GlobalHosts[id].Generation };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(HostHandle host)
    {
        return GlobalHosts[host.Id].Generation == host.Generation;
    }


    public static void Invalidate(HostHandle host)
    {
        if (!IsValid(host)) return;

        GlobalHosts[host.Id].Generation++; // Инкремент поколения защищает от старых хэндлов
        GlobalHosts[host.Id].Next = _firstFree;
        _firstFree = host.Id;
    }

    
    public static void DestroyHost(HostHandle host, HostChainManager chainManager)
    {
        if (!HostManager.IsValid(host)) return;
        
        // Сначала освобождаем все компоненты
        DynamicComponentSystem.FreeChain(host, chainManager);
        
        // Потом освобождаем хост
        Invalidate(host);
    }
}

