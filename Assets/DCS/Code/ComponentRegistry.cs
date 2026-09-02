using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

// Автоматический калькулятор-регистратор типов
public static class ComponentType<T> where T : struct
{
    public static readonly int Id = ComponentRegistry.RegisterNewType<T>();
}

public static class ComponentRegistry
{
    private static int _typeCounter = 0;
    public const int MaxComponentTypes = 200;
    
    // ИСПРАВЛЕНО: Теперь храним строго типизированные для ядра интерфейсы вместо сырых object
    public static readonly IComponentPool[] Pools = new IComponentPool[MaxComponentTypes];
    public static readonly int[] PollTypeIds = new int[MaxComponentTypes];
    public static int PollTypesCount = 0;

    public static void InitializeAllPools()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (var type in types)
        {
            var poolAttribute = type.GetCustomAttribute<DcsPoolAttribute>();
            if (type.IsValueType && poolAttribute != null)
            {
                var genericComponentType = typeof(ComponentType<>).MakeGenericType(type);
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(genericComponentType.TypeHandle);

                if (typeof(IEventData).IsAssignableFrom(type))
                {
                    var idField = genericComponentType.GetField("Id", BindingFlags.Public | BindingFlags.Static);
                    PollTypeIds[PollTypesCount++] = (int)idField.GetValue(null);
                }
            }
        }
        Debug.Log($"<color=green>[DCS SUCCESS]</color> Пулы выделены. Всего типов: {_typeCounter}");
    }

    public static int RegisterNewType<T>() where T : struct
    {
        int newId = _typeCounter++;
        if (newId >= MaxComponentTypes) throw new System.Exception("DCS Error: Превышен лимит типов!");
        
        int capacity = 1000;
        var attr = typeof(T).GetCustomAttribute<DcsPoolAttribute>();
        if (attr != null) capacity = attr.Capacity;
        
        if (typeof(IEventData).IsAssignableFrom(typeof(T)))
        {
            var eventManagerType = typeof(EventManager<>).MakeGenericType(typeof(T));
            Pools[newId] = (IComponentPool)System.Activator.CreateInstance(eventManagerType, capacity);
        }
        else
        {
            Pools[newId] = new ComponentManager<T>(capacity);
        }
        return newId;
    }

    public static ComponentManager<T> GetPool<T>() where T : struct
    {
        return (ComponentManager<T>)Pools[ComponentType<T>.Id];
    }
}