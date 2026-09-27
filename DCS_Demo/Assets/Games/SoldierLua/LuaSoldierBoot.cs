using UnityEngine;

namespace DCS.SoldierCS
{
    /// <summary>
    /// Boot игры SoldierCS.
    ///
    /// Одна строка: Ensure(). Больше ничего.
    ///
    /// Boot ставится в каждую сцену игры. Если Root уже есть — Ensure()
    /// вернёт существующий. Дубликатов не будет.
    ///
    /// Boot НЕ вызывает Root.Update() — Root сам MonoBehaviour,
    /// Unity вызывает его Update.
    /// </summary>
    public sealed class SoldierBoot : MonoBehaviour
    {
        private void OnEnable()
        {
            SoldierRoot.Ensure();
        }
    }
}