using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;

// Базовый тип компонентов
public interface IDcsComponent
{
    int RosterIndex { get; set; }
}

// Интерфейс с инициализацией компонентов
public interface IDcsInitializable
{
    void Init(object  prius);
}

// Ссылка на компонент
public struct DcsHandle 
{
    public int Id; // Индекс в массиве пула
    public int Generation;
    public bool IsNull => Generation == 0;
}

