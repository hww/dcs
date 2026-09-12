using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

namespace DynamicComponent
{
    /// <summary>
    /// Search an animation nu mame
    /// </summary>
    public static class AnimatorFinder
    {
        public static bool HasAnimation(Animator animator, string animationName)
        {
#if UNITY_EDITOR
            if (animator == null || animator.runtimeAnimatorController == null)
                return false;

            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name.Equals(animationName, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
#else
            return true;
#endif
        }

        public static bool HasAnimationState(Animator animator, string stateName)
        {
#if UNITY_EDITOR
            if (animator == null || animator.runtimeAnimatorController == null)
                return false;

            var controller = animator.runtimeAnimatorController as AnimatorController;
            if (controller == null) return false;

            // Проверяем все слои в контроллере
            foreach (var layer in controller.layers)
            {
                // Проверяем все состояния в state machine
                foreach (var state in layer.stateMachine.states)
                {
                    if (state.state.name.Equals(stateName, System.StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                // Также проверяем под-стейт машины
                if (CheckSubStateMachines(animator, layer.stateMachine, stateName))
                    return true;
            }

            return false;
#else
            return true;
#endif
        }
#if UNITY_EDITOR
        private static bool CheckSubStateMachines(Animator animator, AnimatorStateMachine stateMachine, string stateName)
        {
            foreach (var subStateMachine in stateMachine.stateMachines)
            {
                foreach (var state in subStateMachine.stateMachine.states)
                {
                    if (state.state.name.Equals(stateName, System.StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                // Рекурсивно проверяем вложенные state machines
                if (CheckSubStateMachines(animator, subStateMachine.stateMachine, stateName))
                    return true;
            }

            return false;
        }
#endif
    }
}
