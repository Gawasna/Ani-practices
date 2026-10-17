using System.Collections;
using UnityEngine;
using Practice.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Practice.Airplane
{
    /// <summary>
    /// Drives the <c>Takeoff</c> trigger of the airplane Animator so the <c>ThrottleUpAnimation</c>
    /// clip can be demonstrated on demand (button in the demo hub, Space bar, or automatically on start).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class AirplaneTakeoffDemo : MonoBehaviour, IDemoAction
    {
        private static readonly int TakeoffHash = Animator.StringToHash("Takeoff");

        [Header("Auto demo")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private float startDelay = 0.8f;

        private Animator animator;

        public string ButtonLabel => "Takeoff - play ThrottleUpAnimation";

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void Start()
        {
            if (playOnStart)
            {
                StartCoroutine(TriggerAfterDelay());
            }
        }

        private void Update()
        {
            if (WasTakeoffKeyPressed())
            {
                TriggerTakeoff();
            }
        }

        public void Execute()
        {
            TriggerTakeoff();
        }

        /// <summary>Fires the <c>Takeoff</c> trigger, which transitions Idle -> Takeoff in the controller.</summary>
        public void TriggerTakeoff()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator == null)
            {
                return;
            }

            animator.SetTrigger(TakeoffHash);
            Debug.Log("[Practice 2] Takeoff trigger sent - wing rotates 0 -> -15 degrees on X in 1 second.");
        }

        private IEnumerator TriggerAfterDelay()
        {
            yield return new WaitForSeconds(startDelay);
            TriggerTakeoff();
        }

        private static bool WasTakeoffKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }
    }
}
