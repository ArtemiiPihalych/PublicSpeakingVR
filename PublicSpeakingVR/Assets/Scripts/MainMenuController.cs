using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class MainMenuController : MonoBehaviour
{
    public const string DurationMinutesKey = "TrainingDurationMinutes";

    public string trainingSceneName = "SampleScene";
    public int defaultMinutes = 5;
    public int[] minuteOptions = { 1, 3, 5, 7, 10, 15, 20 };
    public string xrOriginResourcePath = "XR Origin (XR Rig)";
    public string xrInteractionSetupResourcePath = "XR Interaction Setup";

    private const float CanvasScale = 0.00145f;
    private const float MenuDistance = 1.2f;
    private const float MenuVerticalOffset = -0.06f;

    private int selectedMinuteIndex;
    private Canvas menuCanvas;
    private TextMeshProUGUI selectedMinutesText;

    private void Awake()
    {
        EnsureXrRig();
        EnsureEventSystem();
        selectedMinuteIndex = Mathf.Max(0, System.Array.IndexOf(minuteOptions, defaultMinutes));
        CreateWorldSpaceMenu();
        StartCoroutine(PlaceMenuWhenXrCameraIsReady());
    }

    private void CreateWorldSpaceMenu()
    {
        GameObject canvasObject = new GameObject("Main Menu World Canvas");
        canvasObject.transform.SetPositionAndRotation(new Vector3(0f, 1.35f, 1.2f), Quaternion.identity);
        canvasObject.transform.localScale = Vector3.one * CanvasScale;

        menuCanvas = canvasObject.AddComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.WorldSpace;
        menuCanvas.worldCamera = GetXrCamera();
        menuCanvas.sortingOrder = 50;

        RectTransform canvasRect = menuCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(560f, 360f);

        canvasObject.AddComponent<GraphicRaycaster>();
        canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        GameObject panel = CreateRect(canvasRect, "Panel", new Vector2(520f, 320f), Vector2.zero);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.075f, 0.095f, 0.105f, 0.96f);

        Shadow panelShadow = panel.AddComponent<Shadow>();
        panelShadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        panelShadow.effectDistance = new Vector2(4f, -4f);

        Image accent = CreateRect(panel.transform, "Accent", new Vector2(520f, 6f), new Vector2(0f, 157f)).AddComponent<Image>();
        accent.color = new Color(0.16f, 0.5f, 0.58f, 1f);

        TextMeshProUGUI eyebrow = CreateText(panel.transform, "Eyebrow", "VR \u0442\u0440\u0435\u043d\u0430\u0436\u0435\u0440", 13, new Vector2(0f, 128f), new Vector2(460f, 20f));
        eyebrow.color = new Color(0.62f, 0.88f, 0.92f, 1f);

        TextMeshProUGUI title = CreateText(panel.transform, "Title", "\u0422\u0440\u0435\u043d\u0430\u0436\u0435\u0440 \u043f\u0443\u0431\u043b\u0438\u0447\u043d\u043e\u0433\u043e\n\u0432\u044b\u0441\u0442\u0443\u043f\u043b\u0435\u043d\u0438\u044f", 27, new Vector2(0f, 84f), new Vector2(480f, 62f));
        title.fontStyle = FontStyles.Bold;
        title.lineSpacing = -10f;

        GameObject timeBlock = CreateRect(panel.transform, "Time Block", new Vector2(360f, 82f), new Vector2(0f, -28f));
        Image timeImage = timeBlock.AddComponent<Image>();
        timeImage.color = new Color(0.045f, 0.06f, 0.07f, 1f);

        CreateText(timeBlock.transform, "Time Label", "\u0412\u0440\u0435\u043c\u044f \u0442\u0440\u0435\u043d\u0438\u0440\u043e\u0432\u043a\u0438", 13, new Vector2(0f, 24f), new Vector2(300f, 18f))
            .color = new Color(0.72f, 0.78f, 0.78f, 1f);

        CreateButton(timeBlock.transform, "Minus", "\u2212", new Vector2(-128f, -9f), new Vector2(52f, 40f), PreviousMinute, true);
        selectedMinutesText = CreateText(timeBlock.transform, "Selected Minutes", "", 25, new Vector2(0f, -5f), new Vector2(170f, 38f));
        selectedMinutesText.fontStyle = FontStyles.Bold;
        CreateButton(timeBlock.transform, "Plus", "+", new Vector2(128f, -9f), new Vector2(52f, 40f), NextMinute, true);

        CreateText(panel.transform, "Hint", "\u0418\u0437\u043c\u0435\u043d\u0438 \u0432\u0440\u0435\u043c\u044f \u043a\u043d\u043e\u043f\u043a\u0430\u043c\u0438 \u2212 \u0438 +", 12, new Vector2(0f, -92f), new Vector2(430f, 20f))
            .color = new Color(0.67f, 0.75f, 0.76f, 1f);

        CreateButton(panel.transform, "Start", "\u041d\u0430\u0447\u0430\u0442\u044c \u0442\u0440\u0435\u043d\u0438\u0440\u043e\u0432\u043a\u0443", new Vector2(0f, -132f), new Vector2(320f, 44f), StartTraining, false);

        RefreshMinutesText();
    }

    private IEnumerator PlaceMenuWhenXrCameraIsReady()
    {
        for (int i = 0; i < 60; i++)
        {
            if (menuCanvas != null)
            {
                Camera xrCamera = GetXrCamera();
                menuCanvas.worldCamera = xrCamera;

                if (xrCamera != null)
                {
                    PlaceMenuInFrontOfCamera(xrCamera);
                    yield break;
                }
            }

            yield return null;
        }
    }

    private void PlaceMenuInFrontOfCamera(Camera xrCamera)
    {
        Vector3 forward = xrCamera.transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.01f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();

        Vector3 position = xrCamera.transform.position + forward * MenuDistance;
        position.y = Mathf.Max(1.15f, xrCamera.transform.position.y + MenuVerticalOffset);

        Transform canvasTransform = menuCanvas.transform;
        canvasTransform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
        canvasTransform.localScale = Vector3.one * CanvasScale;
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

    private int GetSelectedMinutes()
    {
        if (minuteOptions == null || minuteOptions.Length == 0) return Mathf.Max(1, defaultMinutes);
        selectedMinuteIndex = Mathf.Clamp(selectedMinuteIndex, 0, minuteOptions.Length - 1);
        return Mathf.Max(1, minuteOptions[selectedMinuteIndex]);
    }

    private GameObject CreateRect(Transform parent, string name, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject rectObject = new GameObject(name);
        rectObject.transform.SetParent(parent, false);

        RectTransform rect = rectObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        return rectObject;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject textObject = CreateRect(parent, name, size, anchoredPosition);
        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.raycastTarget = false;
        return label;
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction action, bool compact)
    {
        GameObject buttonObject = CreateRect(parent, name, size, anchoredPosition);

        Image image = buttonObject.AddComponent<Image>();
        image.color = compact ? new Color(0.11f, 0.2f, 0.24f, 1f) : new Color(0.08f, 0.34f, 0.42f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.onClick.AddListener(action);

        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = compact ? new Color(0.18f, 0.34f, 0.39f, 1f) : new Color(0.12f, 0.48f, 0.58f, 1f);
        colors.pressedColor = new Color(0.04f, 0.22f, 0.28f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.colorMultiplier = 1f;
        button.colors = colors;

        TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", label, compact ? 24 : 19, Vector2.zero, size);
        text.fontStyle = FontStyles.Bold;
        return button;
    }

    private Camera GetXrCamera()
    {
        GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin != null)
        {
            Camera xrCamera = xrOrigin.GetComponentInChildren<Camera>(true);
            if (xrCamera != null) return xrCamera;
        }

        return Camera.main;
    }

    private void EnsureXrRig()
    {
        if (GameObject.Find("XR Origin (XR Rig)") == null)
        {
            GameObject xrOriginPrefab = Resources.Load<GameObject>(xrOriginResourcePath);
            if (xrOriginPrefab != null)
            {
                GameObject xrOrigin = Instantiate(xrOriginPrefab);
                xrOrigin.name = "XR Origin (XR Rig)";
                xrOrigin.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning($"XR Origin prefab was not found in Resources: {xrOriginResourcePath}");
            }
        }

        if (GameObject.Find("XR Interaction Setup") == null)
        {
            GameObject setupPrefab = Resources.Load<GameObject>(xrInteractionSetupResourcePath);
            if (setupPrefab != null)
            {
                GameObject setup = Instantiate(setupPrefab);
                setup.name = "XR Interaction Setup";
            }
        }
    }

    private void EnsureEventSystem()
    {
        EventSystem existing = FindObjectOfType<EventSystem>();
        if (existing != null)
        {
            StandaloneInputModule standaloneInput = existing.GetComponent<StandaloneInputModule>();
            if (standaloneInput != null) Destroy(standaloneInput);

            if (existing.GetComponent<XRUIInputModule>() == null)
            {
                existing.gameObject.AddComponent<XRUIInputModule>();
            }
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<XRUIInputModule>();
    }
}
