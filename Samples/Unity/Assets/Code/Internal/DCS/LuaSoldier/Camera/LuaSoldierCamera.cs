using UnityEngine;

namespace DCS.LuaSoldier
{
    /// <summary>
    /// Third-person камера. Мышь крутит, идёт за таргетом.
    /// Per-frame. Lua не управляет.
    /// </summary>
    public class LuaSoldierCamera : MonoBehaviour
    {
        [Header("Target")]
        public Transform Target;
        public float LookAtHeight = 1.5f;

        [Header("Layout")]
        public float Distance = 5f;
        public float Height = 2.2f;

        [Header("Mouse")]
        public float MouseSensitivity = 3f;
        public float MinPitch = -30f;
        public float MaxPitch = 60f;
        public bool InvertY = false;

        [Header("Smoothing")]
        public float FollowSpeed = 15f;
        public float RotateSpeed = 15f;

        private float _yaw;
        private float _pitch = 10f;

        public float Yaw => _yaw;
        public float Pitch => _pitch;

        void Start()
        {
            if (Target != null)
                _yaw = Target.eulerAngles.y;
        }

        void LateUpdate()
        {
            if (Target == null) return;

            float mx = Input.GetAxis("Mouse X") * MouseSensitivity;
            float my = Input.GetAxis("Mouse Y") * MouseSensitivity * (InvertY ? 1f : -1f);

            _yaw += mx;
            _pitch += my;
            _pitch = Mathf.Clamp(_pitch, MinPitch, MaxPitch);

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 forward = rotation * Vector3.forward;

            Vector3 targetPoint = Target.position + Vector3.up * LookAtHeight;
            Vector3 desiredPos = targetPoint - forward * Distance + Vector3.up * Height;

            float tp = 1f - Mathf.Exp(-FollowSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPos, tp);

            Quaternion desiredRot = Quaternion.LookRotation(targetPoint - transform.position);
            float tr = 1f - Mathf.Exp(-RotateSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, tr);
        }
    }
}