using DCS.Core;
using UnityEngine;

namespace DCS.Spatial
{
    public interface IPositionSource
    {
        void ForEach(System.Action<Host, Vector3> action);
        bool TryGet(Host host, out Vector3 value);
    }

    public interface INameSource
    {
        bool TryGet(Host host, out string value);
    }

    public interface ITagSource
    {
        bool TryGet(Host host, out uint value);
    }
}