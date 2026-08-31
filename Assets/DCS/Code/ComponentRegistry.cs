using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

// Автоматический калькулятор-регистратор типовpublic 
static class ComponentType<T> where T : struct
{
    public static readonly int Id = ComponentRegistry.RegisterNewType<T>();
}

public static class ComponentRegistry
{
    private static int _typeCounter = 0;
    public const int MaxComponentTypes = 200;
    public static readonly object[] Pools = new object[MaxComponentTypes];
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

                if (type == typeof(LocationEvent))
                {
                    // ИСПРАВЛЕНО: Указаны явные флаги System.Reflection.BindingFlags и правильный генерик-тип
                    var idField = genericComponentType.GetField(nameof(ComponentType<LocationEvent>.Id), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    PollTypeIds[PollTypesCount++] = (int)idField.GetValue(null);
                }
            }
        }

        UnityEngine.Debug.Log($"<color=green>[DCS SUCCESS]</color> Все пулы пред-выделены без спайков! Всего типов: {_typeCounter}");
    }

    public static int RegisterNewType<T>() where T : struct
    {
        int newId = _typeCounter++;
        if (newId >= MaxComponentTypes) throw new System.Exception("DCS Error: Превышен лимит типов!");
        
        int capacity = 1000;
        var attr = typeof(T).GetCustomAttribute<DcsPoolAttribute>();
        if (attr != null) capacity = attr.Capacity;
        
        Pools[newId] = new ComponentManager<T>(capacity);
        return newId;
    }

    public static ComponentManager<T> GetPool<T>() where T : struct
    {
        int typeId = ComponentType<T>.Id;
        return (ComponentManager<T>)Pools[typeId];
    }
}