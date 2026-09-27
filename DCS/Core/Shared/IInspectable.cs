using DCS.Spatial;
using System;
using System.Text;
using UnityEngine;

namespace DCS.Core
{
    /// <summary>
    /// Makes the object inspectable
    /// </summary>
    public interface IInspectable
    {
        public void Inspect(StringBuilder sb, int indentLevel);
    }
}