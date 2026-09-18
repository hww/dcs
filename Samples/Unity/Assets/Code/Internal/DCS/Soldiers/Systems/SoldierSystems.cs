using DCS.Core;
using UnityEngine;

namespace DCS.Soldiers
{
    // ============================================================
    //  PLAYER INPUT — Legacy Input → InputComponent
    //  Работает с конкретным хостом игрока.
    // ============================================================

    public static class PlayerInputSystem
    {
        // A/D в два раза медленнее основного бега
        public const float StrafeMultiplier = 0.5f;

        public static void Update(
            Host playerHost,
            HostChain chain,
            ThirdPersonCamera camera)
        {
            Handle hInput = DCSystem.Get<InputComponent>(playerHost, chain);
            if (hInput.IsNull) return;

            ref InputComponent input = ref DCSystem.ResolveHandle<InputComponent>(hInput);

            // --- WASD ---
            float forward = 0f;
            float strafe = 0f;

            if (Input.GetKey(KeyCode.W)) forward += 1f;
            if (Input.GetKey(KeyCode.S)) forward -= 1f;
            if (Input.GetKey(KeyCode.D)) strafe += StrafeMultiplier;
            if (Input.GetKey(KeyCode.A)) strafe -= StrafeMultiplier;

            input.Forward = forward;
            input.Strafe = strafe;
            input.Fire = Input.GetKey(KeyCode.Space);

            // --- Yaw/Pitch от камеры ---
            if (camera != null)
            {
                input.LookYaw = camera.Yaw;
                input.LookPitch = camera.Pitch;
            }
        }
    }

    // ============================================================
    //  MOVEMENT — обходит всех солдат, обновляет Position, Locomotion
    // ============================================================

    public static class MovementSystem
    {
        public const float RunSpeed = 4.5f;
        public const float StrafeSpeed = RunSpeed * PlayerInputSystem.StrafeMultiplier;  // 2.25

        public static void Update(HostChain chain, Transform cameraTransform, float dt)
        {
            // Оси камеры в плоскости XZ
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0f; camForward.Normalize();
            camRight.y = 0f; camRight.Normalize();

            int count = HostManager.GlobalHosts.Length;
            for (int i = 0; i < count; i++)
            {
                Host host = new Host
                {
                    Id = (ushort)i,
                    Generation = HostManager.GlobalHosts[i].Generation
                };
                if (!HostManager.IsValid(host)) continue;

                Handle hTag = DCSystem.Get<SoldierTag>(host, chain);
                if (hTag.IsNull) continue;

                Handle hInput = DCSystem.Get<InputComponent>(host, chain);
                Handle hPos = DCSystem.Get<PositionComponent>(host, chain);
                Handle hVel = DCSystem.Get<VelocityComponent>(host, chain);
                Handle hLoco = DCSystem.Get<LocomotionComponent>(host, chain);
                if (hInput.IsNull || hPos.IsNull || hVel.IsNull || hLoco.IsNull) continue;

                ref InputComponent input = ref DCSystem.ResolveHandle<InputComponent>(hInput);
                ref PositionComponent pos = ref DCSystem.ResolveHandle<PositionComponent>(hPos);
                ref VelocityComponent vel = ref DCSystem.ResolveHandle<VelocityComponent>(hVel);
                ref LocomotionComponent loco = ref DCSystem.ResolveHandle<LocomotionComponent>(hLoco);

                // Вектор движения в мировых координатах через оси камеры
                Vector3 worldMove = camForward * input.Forward + camRight * input.Strafe;

                // Ограничим магнитуду до 1 (если W и D одновременно)
                if (worldMove.sqrMagnitude > 1f)
                    worldMove = worldMove.normalized;

                vel.Value = worldMove * RunSpeed;
                pos.Value += vel.Value * dt;

                // Локомоция
                float speedSq = worldMove.sqrMagnitude;
                if (input.Fire)
                    loco.Value = ELocomotion.Shoot;
                else if (speedSq > 0.01f)
                    loco.Value = ELocomotion.Run;
                else
                    loco.Value = ELocomotion.Idle;
            }
        }
    }

    // ============================================================
    //  ANIMATION — Locomotion + Combat → Animator ints
    // ============================================================

    public static class SoldierAnimationSystem
    {
        static readonly int PARAM_LOCOMOTION = Animator.StringToHash("Locomotion");
        static readonly int PARAM_COMBAT = Animator.StringToHash("Combat");

        public static void Update(HostChain chain, Animator[] animators)
        {
            int count = HostManager.GlobalHosts.Length;
            for (int i = 0; i < count; i++)
            {
                Host host = new Host
                {
                    Id = (ushort)i,
                    Generation = HostManager.GlobalHosts[i].Generation
                };
                if (!HostManager.IsValid(host)) continue;

                Handle hTag = DCSystem.Get<SoldierTag>(host, chain);
                if (hTag.IsNull) continue;

                Handle hView = DCSystem.Get<ViewComponent>(host, chain);
                Handle hCombat = DCSystem.Get<CombatStateComponent>(host, chain);
                Handle hLoco = DCSystem.Get<LocomotionComponent>(host, chain);
                if (hView.IsNull || hCombat.IsNull || hLoco.IsNull) continue;

                ref ViewComponent view = ref DCSystem.ResolveHandle<ViewComponent>(hView);
                ref CombatStateComponent combat = ref DCSystem.ResolveHandle<CombatStateComponent>(hCombat);
                ref LocomotionComponent loco = ref DCSystem.ResolveHandle<LocomotionComponent>(hLoco);

                if (view.ViewId < 0 || view.ViewId >= animators.Length) continue;
                Animator animator = animators[view.ViewId];
                if (animator == null) continue;

                animator.SetInteger(PARAM_LOCOMOTION, (int)loco.Value);
                animator.SetInteger(PARAM_COMBAT, (int)combat.Value);
            }
        }
    }

    // ============================================================
    //  TRANSFORM SYNC — Position → Transform
    // ============================================================

    public static class TransformSyncSystem
    {
        public static void Update(HostChain chain, Transform[] transforms)
        {
            int count = HostManager.GlobalHosts.Length;
            for (int i = 0; i < count; i++)
            {
                Host host = new Host
                {
                    Id = (ushort)i,
                    Generation = HostManager.GlobalHosts[i].Generation
                };
                if (!HostManager.IsValid(host)) continue;

                Handle hTag = DCSystem.Get<SoldierTag>(host, chain);
                if (hTag.IsNull) continue;

                Handle hView = DCSystem.Get<ViewComponent>(host, chain);
                Handle hPos = DCSystem.Get<PositionComponent>(host, chain);
                Handle hInp = DCSystem.Get<InputComponent>(host, chain);
                if (hView.IsNull || hPos.IsNull || hInp.IsNull) continue;

                ref ViewComponent view = ref DCSystem.ResolveHandle<ViewComponent>(hView);
                ref PositionComponent pos = ref DCSystem.ResolveHandle<PositionComponent>(hPos);
                ref InputComponent input = ref DCSystem.ResolveHandle<InputComponent>(hInp);

                if (view.ViewId < 0 || view.ViewId >= transforms.Length) continue;
                Transform t = transforms[view.ViewId];
                if (t == null) continue;

                t.position = pos.Value;
                t.rotation = Quaternion.Euler(0f, input.LookYaw, 0f);   // <-- ПОВОРОТ
            }
        }
    }
}