using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

public struct Subscription
{
    public HostHandle Receiver;
    public int TargetTypeId;
    public int CallbackId;
    public uint ConditionData;
}

public struct InvokeRecord
{
    public HostHandle Receiver;
    public int CallbackId;
    public int ComponentId;
}


[MessagePool(100)]public struct DamageEvent 
{
    public float Amount;
}

[MessagePool(100)]public struct LocationEvent 
{
    public int ZoneId;
}

public class EventDispatcher
{
    public const int MaxSubscriptions = 5000;
    private readonly Subscription[] _subscriptions = new Subscription[MaxSubscriptions];
    private int _subCount = 0;
    private readonly InvokeRecord[] _invokeList = new InvokeRecord[1000];
    private int _invokeCount = 0;

    public void Subscribe<T>(HostHandle receiver, int callbackId, uint conditionData = 0) where T : struct
    {
        if (_subCount >= MaxSubscriptions) throw new System.Exception("DCS Error: Превышен лимит подписок движка!");
        
        _subscriptions[_subCount] = new Subscription
        {
            Receiver = receiver,
            TargetTypeId = ComponentType<T>.Id,
            CallbackId = callbackId,
            ConditionData = conditionData
        };
        _subCount++;
    }

    // ИСПРАВЛЕНО: Все типы пулов и калькуляторов приведены к строгому дженерику <LocationEvent>
    public void PollSubscriptions()
    {
        _invokeCount = 0;
        ComponentManager<LocationEvent> locPool = ComponentRegistry.GetPool<LocationEvent>();

        for (int i = 0; i < _subCount; i++)
        {
            ref Subscription sub = ref _subscriptions[i];
            if (sub.TargetTypeId == ComponentType<LocationEvent>.Id)
            {
                for (int j = 0; j < locPool.Partition; j++)
                {
                    ref LocationEvent locMsg = ref locPool.Components[j];
                    if (locMsg.ZoneId == (int)sub.ConditionData)
                    {
                        if (_invokeCount >= _invokeList.Length) return;
                        _invokeList[_invokeCount] = new InvokeRecord 
                        {
                            Receiver = sub.Receiver,
                            CallbackId = sub.CallbackId,
                            ComponentId = j
                        };
                        _invokeCount++;
                    }
                }
            }
        }
    }
    
    public void DeliverPollEvents()
    {
        for (int i = 0; i < _invokeCount; i++)
        {
            InvokeRecord record = _invokeList[i];
            UnityEngine.Debug.Log($"<color=cyan>[ПО ПОДПИСКЕ (Poll)]</color> Событие доставлено! " +
            $"Хост №{record.Receiver.Id} уведомлен. Вызван скрипт колбэка №{record.CallbackId}");
        }
    }

    // ИСПРАВЛЕНО: Метод переведен в дженерик-формат для легального проброса типа T в DynamicComponentSystem
    public ComponentHandle SendNotifyEvent<T>(HostHandle receiver, ref Chain chain, object prius = null) where T : struct
    {
        return DynamicComponentSystem.Allocate<T>(receiver, ref chain, prius);
    }
}

