using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace Practice.UI
{
    /// <summary>
    /// Minimalist square button overlay anchored at the bottom-right corner,
    /// labeled with scene numbers (1, 2) to quickly switch between practice scenes.
    /// </summary>
    [DisallowMultipleComponent]
    public class PracticeDemoHub : MonoBehaviour
    {
        private const float ButtonSize = 52f;
        private const float ButtonGap = 10f;
        private const float Margin = 24f;

        [Header("Practice Scenes")]
        [SerializeField] private string[] sceneNames = { "Practice1_XBot", "Practice2_AirplaneWing" };

        [SerializeField] private string[] sceneLabels =
        {
            "1. Mixamo X Bot - Swing Dancing",
            "2. Airplane Wing - ThrottleUp"
        };

        [Header("Status")]
        [SerializeField] private string statusMessage = string.Empty;

        private readonly List<IDemoAction> actions = new List<IDemoAction>();
        private RectTransform panel;
        private Font labelFont;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureHubExists()
        {
            if (FindAnyObjectByType<PracticeDemoHub>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            var host = new GameObject("PracticeDemoHub");
            host.AddComponent<PracticeDemoHub>();
        }

        public void SetStatusMessage(string message)
        {
            statusMessage = message ?? string.Empty;
        }

        private void Start()
        {
            CollectDemoActions();
            BuildInterface();
        }

        private void Update()
        {
            for (int i = 0; i < sceneNames.Length && i < 9; i++)
            {
                if (WasSwitchKeyPressed(i))
                {
                    SwitchToScene(i);
                    return;
                }
            }
        }

        private static bool WasSwitchKeyPressed(int index)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
                return false;
            }

            switch (index)
            {
                case 0:
                    return Keyboard.current.f1Key.wasPressedThisFrame
                        || Keyboard.current.digit1Key.wasPressedThisFrame
                        || Keyboard.current.numpad1Key.wasPressedThisFrame;
                case 1:
                    return Keyboard.current.f2Key.wasPressedThisFrame
                        || Keyboard.current.digit2Key.wasPressedThisFrame
                        || Keyboard.current.numpad2Key.wasPressedThisFrame;
                case 2:
                    return Keyboard.current.f3Key.wasPressedThisFrame
                        || Keyboard.current.digit3Key.wasPressedThisFrame
                        || Keyboard.current.numpad3Key.wasPressedThisFrame;
                case 3:
                    return Keyboard.current.f4Key.wasPressedThisFrame
                        || Keyboard.current.digit4Key.wasPressedThisFrame
                        || Keyboard.current.numpad4Key.wasPressedThisFrame;
            }

            return false;
#else
            switch (index)
            {
                case 0:
                    return Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
                case 1:
                    return Input.GetKeyDown(KeyCode.F2) || Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
                case 2:
                    return Input.GetKeyDown(KeyCode.F3) || Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);
                case 3:
                    return Input.GetKeyDown(KeyCode.F4) || Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4);
            }

            return false;
#endif
        }

        private void CollectDemoActions()
        {
            actions.Clear();

            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            foreach (var behaviour in behaviours)
            {
                if (behaviour is IDemoAction action && !(behaviour is PracticeDemoHub))
                {
                    actions.Add(action);
                }
            }
        }

        private void BuildInterface()
        {
            if (panel != null)
            {
                return;
            }

            labelFont = ResolveUiFont();
            EnsureEventSystem();

            var canvasGo = new GameObject("DemoCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Minimalist container anchored at the bottom-right corner
            var containerGo = new GameObject("ButtonContainer", typeof(RectTransform));
            panel = containerGo.GetComponent<RectTransform>();
            panel.SetParent(canvasGo.transform, false);
            panel.anchorMin = new Vector2(1f, 0f);
            panel.anchorMax = new Vector2(1f, 0f);
            panel.pivot = new Vector2(1f, 0f);
            panel.anchoredPosition = new Vector2(-Margin, Margin);

            int count = sceneNames.Length;
            float totalWidth = count * ButtonSize + (count - 1) * ButtonGap;
            panel.sizeDelta = new Vector2(totalWidth, ButtonSize);

            for (int i = 0; i < count; i++)
            {
                int sceneIndex = i;
                bool isCurrent = sceneNames[i] == SceneManager.GetActiveScene().name;
                CreateSquareButton(panel, (i + 1).ToString(), i, isCurrent, () => SwitchToScene(sceneIndex));
            }
        }

        private void CreateSquareButton(RectTransform parent, string label, int index, bool isCurrent, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Btn_Scene_{index + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(index * (ButtonSize + ButtonGap), 0f);
            rect.sizeDelta = new Vector2(ButtonSize, ButtonSize);

            var image = go.GetComponent<Image>();
            Color baseColor = isCurrent
                ? new Color(0.18f, 0.52f, 0.92f, 0.95f) // Active scene: Vibrant Accent Blue
                : new Color(0.12f, 0.14f, 0.18f, 0.80f); // Inactive scene: Sleek dark translucent
            image.color = baseColor;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            ColorBlock colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = isCurrent ? new Color(0.28f, 0.62f, 1.0f, 1.0f) : new Color(0.24f, 0.28f, 0.35f, 0.95f);
            colors.pressedColor = new Color(0.10f, 0.35f, 0.70f, 1.0f);
            colors.selectedColor = baseColor;
            button.colors = colors;

            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGo.GetComponent<Text>();
            text.font = labelFont;
            text.fontSize = 24;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = label;
        }

        private void SwitchToScene(int index)
        {
            if (sceneNames == null || index < 0 || index >= sceneNames.Length)
            {
                return;
            }

            string target = sceneNames[index];
            if (string.IsNullOrWhiteSpace(target))
            {
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(target))
            {
                Debug.LogError($"[PracticeDemoHub] Scene '{target}' chua co trong Build Settings.");
                return;
            }

            SceneManager.LoadScene(target, LoadSceneMode.Single);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystemGo = new GameObject("DemoEventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            var module = eventSystemGo.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            eventSystemGo.AddComponent<StandaloneInputModule>();
#endif
        }

        private static Font ResolveUiFont()
        {
            Font font = null;
            try
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch (System.Exception)
            {
                // Fall through to the legacy builtin name.
            }

            if (font == null)
            {
                try
                {
                    font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                catch (System.Exception)
                {
                    // Fall through to an OS font.
                }
            }

            return font != null ? font : Font.CreateDynamicFontFromOSFont("Segoe UI", 16);
        }
    }
}
