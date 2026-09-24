using DCS.Core;
using UnityEngine;

namespace DCS.LuaSoldier
{
    // ============================================================
    //  KEYBOARD INPUT — читает клавиатуру, пишет в KeyboardInputComponent
    //  Только для хостов, у которых есть KeyboardInputComponent
    // ============================================================

    public static class KeyboardInputSystem
    {
        public const float StrafeMultiplier = 0.5f;

        public static void Update(HostChain chain, LuaSoldierCamera camera)
        {
            var inputPool = ComponentRegistry.GetPool<KeyboardInputComponent>();
            for (int i = 0; i < inputPool.Partition; i++)
            {
                ref KeyboardInputComponent input = ref inputPool.Components[i];
                Host host = inputPool.Roster[input.RosterIndex].Host;
                if (!HostManager.IsValid(host)) continue;

                float forward = 0f, strafe = 0f;
                if (Input.GetKey(KeyCode.W)) forward += 1f;
                if (Input.GetKey(KeyCode.S)) forward -= 1f;
                if (Input.GetKey(KeyCode.D)) strafe += StrafeMultiplier;
                if (Input.GetKey(KeyCode.A)) strafe -= StrafeMultiplier;

                input.Forward = forward;
                input.Strafe = strafe;
                input.Fire = Input.GetKey(KeyCode.Space);

                // Yaw/Pitch от камеры — если это игрок
                Handle hLook = DCSystem.Get<LookComponent>(host, chain);
                if (!hLook.IsNull && camera != null)
                {
                    ref LookComponent look = ref DCSystem.ResolveHandle<LookComponent>(hLook);
                    look.Yaw = camera.Yaw;
                    look.Pitch = camera.Pitch;
                }
            }
        }
    }

    // ============================================================
    //  MOVEMENT — читает активный InputComponent и двигает PositionComponent
    // ============================================================

    public static class MovementSystem
    {
        public const float RunSpeed = 4.5f;

        public static void Update(HostChain chain, Transform cameraTransform, float dt)
        {
            // Оси камеры в плоскости XZ
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0f; camForward.Normalize();
            camRight.y = 0f; camRight.Normalize();

            var posPool = ComponentRegistry.GetPool<PositionComponent>();
            for (int i = 0; i < posPool.Partition; i++)
            {
                ref PositionComponent pos = ref posPool.Components[i];
                Host host = posPool.Roster[pos.RosterIndex].Host;
                if (!HostManager.IsValid(host)) continue;

                Handle hTag = DCSystem.Get<SoldierTag>(host, chain);
                if (hTag.IsNull) continue;

                // Смотрим, какой input-компонент есть на хосте
                float forward = 0f, strafe = 0f;

                Handle hKb = DCSystem.Get<KeyboardInputComponent>(host, chain);
                if (!hKb.IsNull)
                {
                    ref KeyboardInputComponent kb = ref DCSystem.ResolveHandle<KeyboardInputComponent>(hKb);
                    forward = kb.Forward;
                    strafe = kb.Strafe;
                }
                else
                {
                    Handle hAi = DCSystem.Get<AIInputComponent>(host, chain);
                    if (!hAi.IsNull)
                    {
                        ref AIInputComponent ai = ref DCSystem.ResolveHandle<AIInputComponent>(hAi);
                        forward = ai.Forward;
                        strafe = ai.Strafe;
                    }
                    else
                    {
                        Handle hW = DCSystem.Get<WaterInputComponent>(host, chain);
                        if (!hW.IsNull)
                        {
                            ref WaterInputComponent w = ref DCSystem.ResolveHandle<WaterInputComponent>(hW);
                            forward = w.Forward;
                            strafe = w.Strafe;
                        }
                        else
                        {
                            continue;   // нет источника управления — стоим
                        }
                    }
                }

                // Мировое направление движения
                Vector3 worldMove = camForward * forward + camRight * strafe;
                if (worldMove.sqrMagnitude > 1f) worldMove = worldMove.normalized;

                pos.Value += worldMove * RunSpeed * dt;
            }
        }
    }

    // ============================================================
    //  ANIMATION — какой компонент висит, тот и передаётся в Animator
    // ============================================================

    public static class AnimationSystem
    {
        static readonly int PARAM_LOCOMOTION = Animator.StringToHash("Locomotion");
        static readonly int PARAM_COMBAT = Animator.StringToHash("Combat");
        static readonly int PARAM_FALLTYPE = Animator.StringToHash("FallType");

        public static void Update(HostChain chain)
        {
            var viewPool = ComponentRegistry.GetPool<ViewComponent>();
            for (int i = 0; i < viewPool.Partition; i++)
            {
                ref ViewComponent view = ref viewPool.Components[i];
                Host host = viewPool.Roster[view.RosterIndex].Host;
                if (!HostManager.IsValid(host)) continue;

                Handle hTag = DCSystem.Get<SoldierTag>(host, chain);
                if (hTag.IsNull) continue;

                Animator animator = view.Actor.Animator;
                if (animator == null) continue;

                // На земле?
                Handle hGroundAnim = DCSystem.Get<GroundedAnimationComponent>(host, chain);
                if (!hGroundAnim.IsNull)
                {
                    ref GroundedAnimationComponent a = ref DCSystem.ResolveHandle<GroundedAnimationComponent>(hGroundAnim);
                    animator.SetInteger(PARAM_LOCOMOTION, a.Locomotion);
                    animator.SetInteger(PARAM_COMBAT, a.Combat);
                    continue;
                }

                // Падает?
                Handle hFallAnim = DCSystem.Get<FallingAnimationComponent>(host, chain);
                if (!hFallAnim.IsNull)
                {
                    ref FallingAnimationComponent a = ref DCSystem.ResolveHandle<FallingAnimationComponent>(hFallAnim);
                    animator.SetInteger(PARAM_FALLTYPE, a.FallType);
                    continue;
                }
            }
        }
    }

    // ============================================================
    //  TRANSFORM SYNC — Position + Look → Transform
    // ============================================================

    public static class TransformSyncSystem
    {
        public static void Update(HostChain chain)
        {
            var viewPool = ComponentRegistry.GetPool<ViewComponent>();
            for (int i = 0; i < viewPool.Partition; i++)
            {
                ref ViewComponent view = ref viewPool.Components[i];
                Host host = viewPool.Roster[view.RosterIndex].Host;
                if (!HostManager.IsValid(host)) continue;

                Handle hTag = DCSystem.Get<SoldierTag>(host, chain);
                if (hTag.IsNull) continue;

                Handle hPos = DCSystem.Get<PositionComponent>(host, chain);
                if (hPos.IsNull) continue;

                ref PositionComponent pos = ref DCSystem.ResolveHandle<PositionComponent>(hPos);

                Transform t = view.Actor.transform;
                if (t == null) continue;

                t.position = pos.Value;

                Handle hLook = DCSystem.Get<LookComponent>(host, chain);
                if (!hLook.IsNull)
                {
                    ref LookComponent look = ref DCSystem.ResolveHandle<LookComponent>(hLook);
                    t.rotation = Quaternion.Euler(0f, look.Yaw, 0f);
                }
            }
        }
    }
}