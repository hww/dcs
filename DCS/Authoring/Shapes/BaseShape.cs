using UnityEngine;

namespace DCS.Interaction.Authoring
{
    /// <summary>
    /// Authoring-only spatial shape.
    /// A shape describes geometry; it does not perform spatial queries.
    /// Runtime spatial evaluation belongs to the Spatial subsystem.
    /// </summary>
    public abstract class BaseShape : BaseProxy
    {
        [SerializeField]
        private bool _enabled = true;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>
        /// Shapes are authored relative to their own Transform.
        /// World-space geometry is produced by the Build/Spatial pipeline.
        /// </summary>
        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;
        public Vector3 LossyScale => transform.lossyScale;

        protected virtual void OnDrawGizmosSelected()
        {
            if (!_enabled)
                return;

            DrawShapeGizmo();
        }

        protected abstract void DrawShapeGizmo();
    }
}
