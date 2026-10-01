using DCS.Actors;
using DCS.Core;
using UnityEngine;

namespace DCS.Game.SoldierLua
{

    // ============================================================
    //  KEYBOARD INPUT
    // ============================================================

    public static class KeyboardInputSystem
    {
        public const float StrafeMultiplier = 0.5f;

        public static void Update(HostChain chain)
        {
            var inputPool = ComponentRegistry.GetPool<KeyboardInputComponent>();
            var lookPool = ComponentRegistry.GetPool<LookComponent>();

            for (int i = 0; i < inputPool.Partition; i++)
            {
                ref KeyboardInputComponent input = ref inputPool.Components[i];

                float forward = 0f, strafe = 0f;
                if (Input.GetKey(KeyCode.W)) forward += 1f;
                if (Input.GetKey(KeyCode.S)) forward -= 1f;
                if (Input.GetKey(KeyCode.D)) strafe += StrafeMultiplier;
                if (Input.GetKey(KeyCode.A)) strafe -= StrafeMultiplier;

                input.Forward = forward;
                input.Strafe = strafe;
                input.Fire = Input.GetKey(KeyCode.Space);

                if (Camera.main != null && !input.LookHandle.IsNull)
                {
                    ref LookComponent look = ref lookPool.ResolveHandle(input.LookHandle);
                    Vector3 e = Camera.main.transform.eulerAngles;
                    look.Yaw = e.y;
                    look.Pitch = e.x;
                }
            }
        }
    }

    // ============================================================
    //  MOVEMENT
    // ============================================================

    public static class MovementSystem
    {
        public const float RunSpeed = 4.5f;

        public static void Update(HostChain chain, Transform cameraTransform, float dt)
        {
            Vector3 camForward, camRight;
            if (cameraTransform != null)
            {
                camForward = cameraTransform.forward;
                camRight = cameraTransform.right;
                camForward.y = 0f; camForward.Normalize();
                camRight.y = 0f; camRight.Normalize();
            }
            else
            {
                camForward = Vector3.forward;
                camRight = Vector3.right;
            }

            var animPool = ComponentRegistry.GetPool<AnimationStateComponent>();
            var posPool = ComponentRegistry.GetPool<PositionComponent>();
            var kbPool = ComponentRegistry.GetPool<KeyboardInputComponent>();

            for (int i = 0; i < animPool.Partition; i++)
            {
                ref var anim = ref animPool.Components[i];
                if (anim.PositionHandle.IsNull) continue;

                ref var pos = ref posPool.ResolveHandle(anim.PositionHandle);

                float forward = 0f, strafe = 0f;
                bool fire = false;

                if (!anim.KeyboardInputHandle.IsNull)
                {
                    ref var kb = ref kbPool.ResolveHandle(anim.KeyboardInputHandle);
                    forward = kb.Forward;
                    strafe = kb.Strafe;
                    fire = kb.Fire;
                }

                Vector3 worldMove = camForward * forward + camRight * strafe;
                if (worldMove.sqrMagnitude > 1f) worldMove = worldMove.normalized;
                pos.Position += worldMove * RunSpeed * dt;

                if (anim.IsFalling) anim.Locomotion = 0;
                else if (fire) anim.Locomotion = 2;
                else if (worldMove.sqrMagnitude > 0.01f) anim.Locomotion = 1;
                else anim.Locomotion = 0;
            }
        }
    }

    // ============================================================
    //  ANIMATION
    // ============================================================

    public static class AnimationSystem
    {
        static readonly int PARAM_LOCOMOTION = Animator.StringToHash("Locomotion");
        static readonly int PARAM_COMBAT = Animator.StringToHash("Combat");
        static readonly int PARAM_FALLTYPE = Animator.StringToHash("FallType");

        public static void Update(HostChain chain)
        {
            var pool = ComponentRegistry.GetPool<AnimationStateComponent>();

            for (int i = 0; i < pool.Partition; i++)
            {
                ref var anim = ref pool.Components[i];
                Animator animator = anim.Animator;
                if (animator == null) continue;

                if (anim.IsFalling)
                    animator.SetInteger(PARAM_FALLTYPE, anim.FallType);
                else
                {
                    animator.SetInteger(PARAM_LOCOMOTION, anim.Locomotion);
                    animator.SetInteger(PARAM_COMBAT, anim.Combat);
                }
            }
        }
    }

    // ============================================================
    //  TRANSFORM SYNC
    // ============================================================

    public static class TransformSyncSystem
    {
        public static void Update(HostChain chain)
        {
            var pool = ComponentRegistry.GetPool<TransformStateComponent>();
            var posPool = ComponentRegistry.GetPool<PositionComponent>();
            var lookPool = ComponentRegistry.GetPool<LookComponent>();

            for (int i = 0; i < pool.Partition; i++)
            {
                ref var ts = ref pool.Components[i];
                if (ts.Actor == null) continue;

                Transform t = ts.Actor.transform;
                if (t == null) continue;

                if (!ts.PositionHandle.IsNull)
                {
                    ref var pos = ref posPool.ResolveHandle(ts.PositionHandle);
                    t.position = new Vector3(pos.Position.x, t.position.y, pos.Position.z);
                }

                if (!ts.LookHandle.IsNull)
                {
                    ref var look = ref lookPool.ResolveHandle(ts.LookHandle);
                    t.rotation = Quaternion.Euler(0f, look.Yaw, 0f);
                }
            }
        }
    }
}