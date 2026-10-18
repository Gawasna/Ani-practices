using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text continueButtonText;
    [SerializeField] private TMP_Text restartButtonText;

    [Header("Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        // Setup dynamic text contents as required by Slide 3.3
        if (titleText != null)
        {
            titleText.text = "Flight Result";
        }

        if (messageText != null)
        {
            messageText.text = "Airplane collided with the barrier!";
        }

        if (continueButtonText != null)
        {
            continueButtonText.text = "Continue";
        }

        if (restartButtonText != null)
        {
            restartButtonText.text = "Restart";
        }

        // Bind button actions
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(NextScene);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartScene);
        }

        // Canvas is disabled by default
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(false);
        }
    }

    public void ShowMenu()
    {
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(true);
        }
    }

    public void RestartScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void NextScene()
    {
        int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
        int totalScenes = SceneManager.sceneCountInBuildSettings;

        if (totalScenes > 1)
        {
            int nextIndex = (currentBuildIndex + 1) % totalScenes;
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            // If only one scene is available, reload current scene
            RestartScene();
        }
    }
}
