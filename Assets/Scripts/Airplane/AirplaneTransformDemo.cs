using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Practice.Airplane
{
    /// <summary>
    /// Demonstrates Slide 1.2 Practice 2:
    /// "Move, rotate and scale AirPlane object to test the transform of other airplane's part".
    /// Shows how child parts automatically follow the root Transform without altering their local coordinates.
    /// </summary>
    [DisallowMultipleComponent]
    public class AirplaneTransformDemo : MonoBehaviour
    {
        [Header("Auto Motion Demonstration")]
        [SerializeField] private bool autoDemonstration = true;
        [SerializeField] private float flightSpeed = 2.0f;
        [SerializeField] private float turnSpeed = 45.0f;
        [SerializeField] private float bobbingAmplitude = 0.5f;

        [Header("Interactive Speed")]
        [SerializeField] private float moveSpeed = 5.0f;
        [SerializeField] private float rotateSpeed = 60.0f;

        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Vector3 initialScale;

        private void Awake()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            initialScale = transform.localScale;
        }

        private void Update()
        {
            if (autoDemonstration)
            {
                RunAutoFlight();
            }
            else
            {
                HandleManualInput();
            }

            // Press T to toggle auto demonstration
            if (WasKeyTogglePressed())
            {
                autoDemonstration = !autoDemonstration;
            }

            // Press R to reset transform
            if (WasResetKeyPressed())
            {
                ResetTransform();
            }
        }

        private void RunAutoFlight()
        {
            float time = Time.time;

            // Gentle bank & turn
            float yaw = Mathf.Sin(time * 0.5f) * turnSpeed;
            float pitch = Mathf.Sin(time * 1.0f) * (turnSpeed * 0.25f);
            float roll = -Mathf.Sin(time * 0.5f) * (turnSpeed * 0.5f);
            transform.rotation = initialRotation * Quaternion.Euler(pitch, yaw, roll);

            // Subtle bobbing up and down
            float yOffset = Mathf.Sin(time * 1.5f) * bobbingAmplitude;
            transform.position = initialPosition + new Vector3(Mathf.Sin(time * 0.5f) * flightSpeed, yOffset, 0f);
        }


        private void HandleManualInput()
        {
            float dt = Time.deltaTime;
            Vector3 move = Vector3.zero;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) move.z += 1f;
                if (Keyboard.current.sKey.isPressed) move.z -= 1f;
                if (Keyboard.current.aKey.isPressed) move.x -= 1f;
                if (Keyboard.current.dKey.isPressed) move.x += 1f;
                if (Keyboard.current.qKey.isPressed) transform.Rotate(Vector3.up, -rotateSpeed * dt, Space.World);
                if (Keyboard.current.eKey.isPressed) transform.Rotate(Vector3.up, rotateSpeed * dt, Space.World);
            }
#else
            if (Input.GetKey(KeyCode.W)) move.z += 1f;
            if (Input.GetKey(KeyCode.S)) move.z -= 1f;
            if (Input.GetKey(KeyCode.A)) move.x -= 1f;
            if (Input.GetKey(KeyCode.D)) move.x += 1f;
            if (Input.GetKey(KeyCode.Q)) transform.Rotate(Vector3.up, -rotateSpeed * dt, Space.World);
            if (Input.GetKey(KeyCode.E)) transform.Rotate(Vector3.up, rotateSpeed * dt, Space.World);
#endif

            if (move.sqrMagnitude > 0.01f)
            {
                transform.Translate(move.normalized * (moveSpeed * dt), Space.World);
            }
        }

        public void ResetTransform()
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
            transform.localScale = initialScale;
        }

        private static bool WasKeyTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.T);
#endif
        }

        private static bool WasResetKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }

        private void OnGUI()
        {
            var rect = new Rect(20, 20, 380, 160);
            GUI.Box(rect, "Slide 1.2 - Practice 2: Nested Transform Tester");

            GUILayout.BeginArea(new Rect(30, 50, 360, 125));
            GUILayout.Label($"Root Position: {transform.position:F2}");
            GUILayout.Label($"Root Rotation: {transform.eulerAngles:F1}");
            GUILayout.Label($"Mode: {(autoDemonstration ? "Auto Flight Demo (Press T for Manual)" : "Manual WASD + QE (Press T for Auto)")}");
            GUILayout.Label("Phim R: Reset Transform | Phim T: Chuyen che do");
            GUILayout.EndArea();
        }
    }
}
