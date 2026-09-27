using ParkourFPS;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

// Tutorial-scene-only pause menu. UI is built once as real child GameObjects (not purely at
// runtime) so it shows up in the Hierarchy/Inspector in Edit Mode and can be tweaked freely.
[ExecuteAlways]
public class Esc : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "01_MainMenu";

    [Header("UI (auto-created once; safe to re-style/move by hand afterwards)")]
    [SerializeField] private GameObject pauseCanvas;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;

    private bool isPaused;
    private PlayerControllerScript playerController;

    private void Awake()
    {
        if (pauseCanvas == null) BuildPauseUI();

        if (!Application.isPlaying) return;

        // Found once here since it only exists in this scene; never persisted/shared across scenes.
        playerController = FindFirstObjectByType<PlayerControllerScript>();

        pauseCanvas.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        // Non-persistent listeners don't survive domain reloads, so re-wire them every enable.
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(Resume);
            resumeButton.onClick.AddListener(Resume);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitToMainMenu);
            quitButton.onClick.AddListener(QuitToMainMenu);
        }
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    private void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        pauseCanvas.SetActive(true);

        // Time.timeScale = 0 already freezes physics-driven movement, but mouse look reads
        // raw input every Update() regardless of timeScale, so it needs an explicit toggle.
        if (playerController != null) playerController.InputEnabled = false;
    }

    private void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        pauseCanvas.SetActive(false);

        if (playerController != null) playerController.InputEnabled = true;
    }

    private void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void BuildPauseUI()
    {
        // Scene has no pre-built UI, so create the canvas/EventSystem needed for the menu to work.
        if (EventSystem.current == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        pauseCanvas = new GameObject("PauseCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        pauseCanvas.transform.SetParent(transform, false);

        var canvas = pauseCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = pauseCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        Image background = CreateImage("Background", pauseCanvas.transform, new Color(0f, 0f, 0f, 0.75f));
        StretchFull(background.rectTransform);

        Text title = CreateText("Title", background.transform, "PAUSED", 64);
        title.rectTransform.anchorMin = new Vector2(0.5f, 0.75f);
        title.rectTransform.anchorMax = new Vector2(0.5f, 0.75f);
        title.rectTransform.sizeDelta = new Vector2(600, 100);
        title.rectTransform.anchoredPosition = Vector2.zero;

        resumeButton = CreateButton("ResumeButton", background.transform, "Resume", new Vector2(0, 20));
        quitButton = CreateButton("QuitButton", background.transform, "Quit to Main Menu", new Vector2(0, -60));

        pauseCanvas.SetActive(false);

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    private static Text CreateText(string name, Transform parent, string content, int fontSize, Color? color = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        var text = go.GetComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color ?? Color.white;
        return text;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(320, 60);
        rect.anchoredPosition = anchoredPosition;

        go.GetComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f, 0.95f);

        Text buttonLabel = CreateText(name + "Label", go.transform, label, 28, Color.black);
        StretchFull(buttonLabel.rectTransform);

        return go.GetComponent<Button>();
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
