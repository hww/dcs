using DCS.Spatial;
using System;
using System.Text;
using UnityEngine;

namespace DCS.Core
{
    /// <summary>
    /// Give the object lifecycle menthods
    /// </summary>
    public interface ILifeCycle
    {
        void Birth();
        void Kill();
    }

    /// <summary>
    /// Опциональный интерфейс: если MonoBehaviour его реализует,
    /// ViewService передаст ему контекст спавна перед Birth().
    /// Позволяет актёру получить внешние зависимости без синглтонов.
    /// </summary>
    public interface IBirthContext
    {
        void SetContext(object context);
    }
}