using UnityEngine;
using Practice.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Practice.Animation
{
    [RequireComponent(typeof(Animator))]
    public class CharacterAnimationController : MonoBehaviour, IDemoAction
    {
        [Header("Components")]
        [SerializeField] private Animator animator;

        [Header("Runtime State")]
        [SerializeField] private CharacterState currentState = CharacterState.Dance;

        // Cached Animator parameter hashes for optimal performance
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsDancingHash = Animator.StringToHash("IsDancing");
        private static readonly int DanceTriggerHash = Animator.StringToHash("DanceTrigger");
        private static readonly int StopTriggerHash = Animator.StringToHash("StopTrigger");

        public CharacterState CurrentState => currentState;

        /// <summary>
        /// Sets the state the component starts in, without touching the Animator. Used by the Editor
        /// pipeline when it bakes the prefab, so the initial state is compiled in instead of being
        /// written into a serialized field by name.
        /// </summary>
        public void SetInitialState(CharacterState state)
        {
            currentState = state;
        }

        /// <summary>Label used by the practice demo hub button.</summary>
        public string ButtonLabel => currentState == CharacterState.Dance
            ? "Swing Dancing -> Idle"
            : "Idle -> Swing Dancing";

        /// <summary>Demo hub entry point for the same action as the Space bar shortcut.</summary>
        public void Execute()
        {
            ToggleDance();
        }

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        private void Start()
        {
            ApplyCurrentState();
        }

        private void Update()
        {
            HandleQuickTestInput();
        }

        /// <summary>
        /// Transitions to a new character state and updates Animator parameters.
        /// Foundation for Practice 2 State Machine logic.
        /// </summary>
        public void TransitionToState(CharacterState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            ApplyCurrentState();
            Debug.Log($"[State Machine] State changed to: {newState}");
        }

        private void ApplyCurrentState()
        {
            if (animator == null) return;

            switch (currentState)
            {
                case CharacterState.Dance:
                    animator.SetBool(IsDancingHash, true);
                    animator.SetFloat(SpeedHash, 0f);
                    if (animator.isActiveAndEnabled && !animator.GetCurrentAnimatorStateInfo(0).IsName("SwingDancing"))
                    {
                        animator.Play("SwingDancing", 0, 0f);
                    }
                    break;
                case CharacterState.Idle:
                    animator.SetBool(IsDancingHash, false);
                    animator.SetFloat(SpeedHash, 0f);
                    if (animator.isActiveAndEnabled && !animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
                    {
                        animator.Play("Idle", 0, 0f);
                    }
                    break;
                case CharacterState.Walk:
                    animator.SetBool(IsDancingHash, false);
                    animator.SetFloat(SpeedHash, 1f);
                    break;
                case CharacterState.Run:
                    animator.SetBool(IsDancingHash, false);
                    animator.SetFloat(SpeedHash, 2f);
                    break;
            }
        }

        /// <summary>
        /// Quick keyboard shortcut to test animation transitions during Play Mode.
        /// Press Space to Toggle Dance / Idle.
        /// </summary>
        private void HandleQuickTestInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                ToggleDance();
            }
#else
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ToggleDance();
            }
#endif
        }

        public void ToggleDance()
        {
            var next = (currentState == CharacterState.Dance) ? CharacterState.Idle : CharacterState.Dance;
            TransitionToState(next);
        }
    }
}
