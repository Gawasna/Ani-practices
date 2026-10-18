using UnityEngine;

public class EnterGame : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private Transform targetPlane;

    private bool isTriggered = false;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = Object.FindAnyObjectByType<GameManager>();
        }

        if (targetPlane == null)
        {
            GameObject planeObj = GameObject.Find("AirPlane");
            if (planeObj != null)
            {
                targetPlane = planeObj.transform;
            }
        }
    }

    private void Update()
    {
        if (isTriggered) return;

        // Proximity safety check in case kinematic animation skips discrete trigger frame
        if (targetPlane != null)
        {
            float distZ = Mathf.Abs(targetPlane.position.z - transform.position.z);
            float distY = Mathf.Abs(targetPlane.position.y - transform.position.y);
            if (distZ < 2.5f && distY < 6.0f)
            {
                TriggerHit(targetPlane.gameObject);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;

        bool isAirplane = other.CompareTag("Player") ||
                          other.name.Contains("Airplane") ||
                          other.transform.root.name.Contains("AirPlane") ||
                          other.transform.root.name.Contains("Airplane");

        if (isAirplane)
        {
            TriggerHit(other.gameObject);
        }
    }

    private void TriggerHit(GameObject planeObj)
    {
        if (isTriggered) return;
        isTriggered = true;

        // Stop plane animation / motion on collision
        Animator animator = planeObj.GetComponentInParent<Animator>();
        if (animator != null)
        {
            animator.speed = 0f;
        }

        Rigidbody rb = planeObj.GetComponentInParent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Display popup menu
        if (gameManager != null)
        {
            gameManager.ShowMenu();
        }
        else if (menuCanvas != null)
        {
            menuCanvas.SetActive(true);
        }

        Debug.Log("[EnterGame] Collision detected! Pop-up menu displayed.");
    }
}
