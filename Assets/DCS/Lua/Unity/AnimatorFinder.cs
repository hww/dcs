using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

namespace DynamicComponent
{
    public class AnimatorFinder
    {
        private readonly Animator _animator;

        public AnimatorFinder(Animator animator)
        {
            _animator = animator;
        }

        public bool HasAnimation(string animationName)
        {
#if UNITY_EDITOR
            if (_animator == null || _animator.runtimeAnimatorController == null)
                return false;

            foreach (var clip in _animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name.Equals(animationName, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
#else
            return true;
#endif
        }

        public bool HasAnimationState(string stateName)
        {
#if UNITY_EDITOR
            if (_animator == null || _animator.runtimeAnimatorController == null)
                return false;

            var controller = _animator.runtimeAnimatorController as AnimatorController;
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
                if (CheckSubStateMachines(layer.stateMachine, stateName))
                    return true;
            }

            return false;
#else
            return true;
#endif
        }
#if UNITY_EDITOR
        private bool CheckSubStateMachines(AnimatorStateMachine stateMachine, string stateName)
        {
            foreach (var subStateMachine in stateMachine.stateMachines)
            {
                foreach (var state in subStateMachine.stateMachine.states)
                {
                    if (state.state.name.Equals(stateName, System.StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                // Рекурсивно проверяем вложенные state machines
                if (CheckSubStateMachines(subStateMachine.stateMachine, stateName))
                    return true;
            }

            return false;
        }
#endif
    }
}
