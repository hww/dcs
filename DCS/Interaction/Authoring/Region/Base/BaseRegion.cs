using System.Collections.Generic;
using UnityEngine;
using DCS.Spatial;
using DCS.Core;
namespace DCS.Interaction.Authoring
{
    public abstract class BaseRegion : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField] private bool _enabled = true;
        private readonly List<BaseShape> _shapes = new List<BaseShape>();
        private bool _cached;
        public string Key { get => _key; set => _key = value; }
        public bool Enabled { get => _enabled; set => _enabled = value; }
        public IReadOnlyList<BaseShape> Shapes { get { CacheShapes(); return _shapes; } }
        public int ShapeCount { get { CacheShapes(); return _shapes.Count; } }
        public ESpatialObjectType SpatialType => ESpatialObjectType.Region;
        public void RefreshShapes() { _cached = false; CacheShapes(); }
        protected virtual void OnTransformChildrenChanged() => _cached = false;
        protected virtual void OnValidate() => _cached = false;
        private void CacheShapes()
        {
            if (_cached) return;
            _shapes.Clear();
            BaseShape[] found = GetComponentsInChildren<BaseShape>(true);
            for (int i = 0; i < found.Length; i++) if (found[i] != null && found[i].transform.parent == transform) _shapes.Add(found[i]);
            _cached = true;
        }
    }
}
