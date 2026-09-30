using DCS.Actors;
using DCS.Core;
using UnityEngine;

namespace DCS.Game.SoldierLua
{
    // ============================================================
    //  KEYBOARD INPUT — читает клавиатуру, пишет в KeyboardInputComponent
    //  Только для хостов, у которых есть KeyboardInputComponent
    // ============================================================

    public static class KeyboardInputSystem
    {
        public const float StrafeMultiplier = 0.5f;

        public static void Update(HostChain chain)   // ← без camera
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

                // Yaw/Pitch — откуда? Сейчас нет LuaSoldierCamera.
                // Вариант 1: не трогать LookComponent вообще.
                // Вариант 2: брать из Camera.main.
                if (Camera.main != null)
                {
                    Handle hLook = DCSystem.Get<LookComponent>(host, chain);
                    if (!hLook.IsNull)
                    {
                        ref LookComponent look = ref DCSystem.ResolveHandle<LookComponent>(hLook);
                        Vector3 e = Camera.main.transform.eulerAngles;
                        look.Yaw = e.y;
                        look.Pitch = e.x;
                    }
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

                Handle hType = DCSystem.Get<ArchetypeComponent>(host, chain);
                if (hType.IsNull) continue;

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

                pos.Position += worldMove * RunSpeed * dt;
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
            var posPool = ComponentRegistry.GetPool<PositionComponent>();
            for (int i = 0; i < posPool.Partition; i++)
            {
                ref PositionComponent pos = ref posPool.Components[i];
                Host host = posPool.Roster[pos.RosterIndex].Host;
                if (!HostManager.IsValid(host)) continue;

                Handle hType = DCSystem.Get<ArchetypeComponent>(host, chain);
                if (hType.IsNull) continue;

                var reference = HostManager.GetActor(host);
                if (reference is not Actor actor) continue;

                Animator animator = actor.Animator;
                if (animator == null) continue;

                Handle hGroundAnim = DCSystem.Get<GroundedAnimationComponent>(host, chain);
                if (!hGroundAnim.IsNull)
                {
                    ref GroundedAnimationComponent a = ref DCSystem.ResolveHandle<GroundedAnimationComponent>(hGroundAnim);
                    animator.SetInteger(PARAM_LOCOMOTION, a.Locomotion);
                    animator.SetInteger(PARAM_COMBAT, a.Combat);
                    continue;
                }

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
            var posPool = ComponentRegistry.GetPool<PositionComponent>();
            for (int i = 0; i < posPool.Partition; i++)
            {
                ref PositionComponent pos = ref posPool.Components[i];
                Host host = posPool.Roster[pos.RosterIndex].Host;
                if (!HostManager.IsValid(host)) continue;

                Handle hType = DCSystem.Get<ArchetypeComponent>(host, chain);
                if (hType.IsNull) continue;

                var reference = HostManager.GetActor(host);
                if (reference is not BaseActor actor) continue;

                Transform t = actor.transform;
                if (t == null) continue;

                t.position = pos.Position;

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
