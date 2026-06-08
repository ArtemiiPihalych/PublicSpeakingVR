using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public const string DurationMinutesKey = "TrainingDurationMinutes";

    public string trainingSceneName = "SampleScene";
    public int defaultMinutes = 5;
    public int[] minuteOptions = { 1, 3, 5, 7, 10, 15, 20 };

    private int selectedMinuteIndex;
    private TextMeshProUGUI selectedMinutesText;

    private void Awake()
    {
        EnsureCamera();
        EnsureEventSystem();
        selectedMinuteIndex = Mathf.Max(0, System.Array.IndexOf(minuteOptions, defaultMinutes));
        CreateMenu();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CreateMenu()
    {
        GameObject canvasObject = new GameObject("Main Menu Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();
        canvasObject.AddComponent<Image>().color = new Color(0.055f, 0.07f, 0.075f, 1f);

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        GameObject panel = new GameObject("Menu Panel");
        panel.transform.SetParent(root, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(640f, 430f);
        panelRect.anchoredPosition = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0.12f, 0.15f, 0.16f, 0.97f);

        CreateText(panel.transform, "Title", "\u0422\u0440\u0435\u043d\u0430\u0436\u0435\u0440 \u043f\u0443\u0431\u043b\u0438\u0447\u043d\u043e\u0433\u043e \u0432\u044b\u0441\u0442\u0443\u043f\u043b\u0435\u043d\u0438\u044f", 30, new Vector2(0f, 145f), new Vector2(560f, 70f));
        CreateText(panel.transform, "Hint", "\u0412\u0440\u0435\u043c\u044f \u0442\u0440\u0435\u043d\u0438\u0440\u043e\u0432\u043a\u0438", 22, new Vector2(0f, 75f), new Vector2(420f, 42f));

        CreateButton(panel.transform, "Minus", "-", new Vector2(-175f, 15f), PreviousMinute, new Vector2(70f, 58f));
        selectedMinutesText = CreateText(panel.transform, "Selected Minutes", "", 30, new Vector2(0f, 15f), new Vector2(230f, 58f));
        selectedMinutesText.color = new Color(0.98f, 0.98f, 0.96f, 1f);
        CreateButton(panel.transform, "Plus", "+", new Vector2(175f, 15f), NextMinute, new Vector2(70f, 58f));

        CreateButton(panel.transform, "Start Button", "\u041d\u0430\u0447\u0430\u0442\u044c \u0442\u0440\u0435\u043d\u0438\u0440\u043e\u0432\u043a\u0443", new Vector2(0f, -105f), StartTraining, new Vector2(360f, 64f));
        RefreshMinutesText();
    }

    private void PreviousMinute()
    {
        selectedMinuteIndex = (selectedMinuteIndex - 1 + minuteOptions.Length) % minuteOptions.Length;
        RefreshMinutesText();
    }

    private void NextMinute()
    {
        selectedMinuteIndex = (selectedMinuteIndex + 1) % minuteOptions.Length;
        RefreshMinutesText();
    }

    private void RefreshMinutesText()
    {
        if (selectedMinutesText != null)
        {
            selectedMinutesText.text = $"{GetSelectedMinutes()} \u043c\u0438\u043d.";
        }
    }

    private void StartTraining()
    {
        PlayerPrefs.SetInt(DurationMinutesKey, GetSelectedMinutes());
        PlayerPrefs.Save();
        SceneManager.LoadScene(trainingSceneName);
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action, Vector2 size)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.39f, 0.45f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        CreateText(buttonObject.transform, "Label", label, 24, Vector2.zero, rect.sizeDelta);
        return button;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        return label;
    }

    private int GetSelectedMinutes()
    {
        if (minuteOptions == null || minuteOptions.Length == 0) return Mathf.Max(1, defaultMinutes);
        selectedMinuteIndex = Mathf.Clamp(selectedMinuteIndex, 0, minuteOptions.Length - 1);
        return Mathf.Max(1, minuteOptions[selectedMinuteIndex]);
    }

    private void EnsureCamera()
    {
        if (Camera.main != null) return;

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.055f, 0.07f, 0.075f, 1f);
        cameraObject.AddComponent<AudioListener>();
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }
}
