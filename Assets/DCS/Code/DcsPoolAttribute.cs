using System;

// Базовый системный атрибут для настройки размера пула
[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public class DcsPoolAttribute : Attribute
{
    public int Capacity { get; }

    public DcsPoolAttribute(int capacity = 1000)
    {
        Capacity = capacity;
    }
}

// Атрибут для персистентных компонентов (например, RotatorComponent)
public class ComponentPoolAttribute : DcsPoolAttribute
{
    public ComponentPoolAttribute(int capacity = 1000) : base(capacity) { }
}

// Атрибут для игровых событий и сообщений (например, DamageEvent, LocationEvent)
public class MessagePoolAttribute : DcsPoolAttribute
{
    public MessagePoolAttribute(int capacity = 500) : base(capacity) { }
}
