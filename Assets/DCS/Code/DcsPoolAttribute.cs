using System;

// Базовый системный атрибут для настройки размера пула
[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public class DcsPoolAttribute : Attribute
{
    public int Capacity { get; }
    public uint Mask { get; }
    public EUpdateStage UpdateStage { get; }
    public EAsyncUpdateStage AsyncUpdateStage { get; }

    public DcsPoolAttribute(int capacity = 1000, 
        EUpdateStage updateStage = EUpdateStage.Update, 
        EAsyncUpdateStage asyncUpdateStage = EAsyncUpdateStage.None, 
        uint mask = 0)
    {
        Capacity = capacity;
        UpdateStage = updateStage;
        AsyncUpdateStage = asyncUpdateStage;
        Mask = mask;
    }
}

// Атрибут для персистентных компонентов (например, RotatorComponent)
public class ComponentPoolAttribute : DcsPoolAttribute
{
    public ComponentPoolAttribute(int capacity = 1000, 
        EUpdateStage updateStage = EUpdateStage.Update, 
        EAsyncUpdateStage asyncUpdateStage = EAsyncUpdateStage.None, 
        uint mask = 0) : base(capacity, updateStage, asyncUpdateStage, mask) { }
}

// Атрибут для игровых событий и сообщений (например, DamageEvent, LocationEvent)
public class MessagePoolAttribute : DcsPoolAttribute
{
    public MessagePoolAttribute(int capacity = 1000, 
        EUpdateStage updateStage = EUpdateStage.Update, 
        EAsyncUpdateStage asyncUpdateStage = EAsyncUpdateStage.None, 
        uint mask = 0) : base(capacity, updateStage, asyncUpdateStage, mask) { }    
}
