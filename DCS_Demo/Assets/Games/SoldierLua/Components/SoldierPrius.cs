using DCS.Actors;
using DCS.Core;
using UnityEngine;

namespace DCS.Game.SoldierLua
{
    /// <summary>
    /// Контекст создания солдата.
    /// Передаётся в Init каждого компонента солдата.
    /// Класс, не структура — чтобы избежать боксинга при передаче через object.
    /// </summary>
    public sealed class SoldierPrius
    {
        // --- Идентификация ---
        public Host Host;
        public Domain Domain;
        public GameObject GameObject;
        public Actor Actor;

        // --- Ссылки на компоненты (Handle'ы) ---
        public Handle PositionHandle;
        public Handle LookHandle;
        public Handle KeyboardInputHandle;
        public Handle AnimationStateHandle;
        public Handle TransformStateHandle;

        // --- Параметры ---
        public string SoldierName;
        public Vector3 SpawnPosition;
        public string Archetype;
    }
}