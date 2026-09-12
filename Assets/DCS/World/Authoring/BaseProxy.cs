using System.Collections;
using System.Text;
using UnityEngine;

namespace DynamicComponent
{
    /// <summary>
    /// Base class for a component in the game
    /// </summary>
    public class BaseProxy : MonoBehaviour
    {

        public virtual void Inspect(StringBuilder sb, int indentLevel)
        {
            sb.AppendLine($"[BaseProxy]");
        }
    }
}