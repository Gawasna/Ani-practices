using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text yesButtonText;
    [SerializeField] private TMP_Text noButtonText;

    [Header("Buttons")]
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private void Awake()
    {
        // Dynamic content loaded from code: "Do you want to continue", 1 Co, 2 Khong
        if (questionText != null)
        {
            questionText.text = "Do you want to continue";
        }

        if (yesButtonText != null)
        {
            yesButtonText.text = "1. Có";
        }

        if (noButtonText != null)
        {
            noButtonText.text = "2. Không";
        }

        // Button actions
        if (yesButton != null)
        {
            yesButton.onClick.RemoveAllListeners();
            yesButton.onClick.AddListener(NextScene);
        }

        if (noButton != null)
        {
            noButton.onClick.RemoveAllListeners();
            noButton.onClick.AddListener(RestartScene);
        }

        // Hide canvas initially
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
            RestartScene();
        }
    }
}
