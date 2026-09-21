using UnityEngine;

namespace DCS.Soldiers
{
    /// <summary>
    /// Third-person камера. Мышь управляет yaw/pitch.
    /// Позиция — сзади и сверху от Target.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
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
        public float FollowSpeed = 12f;
        public float RotateSpeed = 15f;

        private float _yaw;
        private float _pitch = 10f;

        void Start()
        {
            if (Target != null)
                _yaw = Target.eulerAngles.y;
        }

        void LateUpdate()
        {
            if (Target == null) return;

            // --- Мышь ---
            float mx = Input.GetAxis("Mouse X") * MouseSensitivity;
            float my = Input.GetAxis("Mouse Y") * MouseSensitivity * (InvertY ? 1f : -1f);

            _yaw += mx;
            _pitch += my;
            _pitch = Mathf.Clamp(_pitch, MinPitch, MaxPitch);

            // --- Позиция ---
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 forward = rotation * Vector3.forward;

            Vector3 targetPoint = Target.position + Vector3.up * LookAtHeight;
            Vector3 desiredPos = targetPoint - forward * Distance + Vector3.up * Height;

            transform.position = Vector3.Lerp(
                transform.position, desiredPos, FollowSpeed * Time.deltaTime);

            // --- Направление ---
            Quaternion desiredRot = Quaternion.LookRotation(targetPoint - transform.position);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, desiredRot, RotateSpeed * Time.deltaTime);
        }

        // --- Публичный API для PlayerInputSystem ---
        public float Yaw => _yaw;
        public float Pitch => _pitch;
    }
}