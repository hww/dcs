using UnityEngine;

namespace DynamicComponent
{
    /// <summary>
    /// Locator — a spatial coordinate anchor node within a scene context partition map.
    /// Used natively by systems as target vectors, interest checkpoints, or transformation markers.
    /// </summary>
    public class Locator : EntityActor
    {
        public override void Birth()
        {
            // Resolve geographic hierarchy bounds prior to executing registry framework insertion
            FindParentZone();
            base.Birth();
        }

        protected virtual void FindParentZone()
        {
            if (Zone == null)
                Zone = transform.GetComponentInParent<Zone>();

            if (Zone == null)
                Debug.LogWarning($"[Locator] Runtime warning: '{name}' has no parent Zone object in hierarchy.", this);
        }

        public Vector3 GetWorldPosition() => transform.position;

        public Vector3 GetLocalPosition()
        {
            if (Zone != null)
                return Zone.transform.InverseTransformPoint(transform.position);
            return transform.localPosition;
        }

        public override void Inspect()
        {
            base.Inspect();
            Debug.Log($"[Locator Details] Linked Zone: {Zone?.name ?? "None"}");
        }
    }
}
