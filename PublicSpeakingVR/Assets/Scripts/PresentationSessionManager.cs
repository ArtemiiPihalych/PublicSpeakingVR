using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class PresentationSessionManager : MonoBehaviour
{
    private const string DurationMinutesKey = "TrainingDurationMinutes";
    private const float ResultScreenDelaySeconds = 3f;

    private enum ResultScreenMode
    {
        TimeExpiredBeforeLastSlide,
        FinishedOnLastSlideInTime
    }

    public SlideChanger slideChanger;
    public SpeakerTimer speakerTimer;
    public AudienceReactionController audience;
    public Transform menuAnchor;
    public string mainMenuSceneName = "MainMenu";
    public bool createMenuOnStart = true;
    public bool autoStartTrainingScene = true;
    public KeyCode menuToggleKey = KeyCode.M;
    public int defaultTrainingMinutes = 5;
    public int[] trainingMinuteOptions = { 1, 3, 5, 7, 10, 15, 20 };

    private Canvas menuCanvas;
    private GameObject setupPanel;
    private GameObject controlsPanel;
    private TextMeshProUGUI statsText;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI timerButtonText;
    private TMP_Dropdown minuteDropdown;
    private Canvas resultCanvas;
    private Coroutine resultScreenRoutine;
    private float sessionElapsed;
    private int slideChangesAtStart;
    private bool sessionStarted;
    private bool sessionFinished;
    private bool successfulFinalReactionPlayed;
    private bool resultScreenScheduled;

    private void Awake()
    {
        ResolveReferences();
        ImproveSceneLighting();
        EnsureEventSystem();
        EnsureVrMovement();

        if (createMenuOnStart)
        {
            CreateMenu();
        }
    }

    private void Start()
    {
        if (!createMenuOnStart && autoStartTrainingScene)
        {
            StartSession();
        }
    }

    private void OnEnable()
    {
        if (slideChanger != null) slideChanger.SlideChanged += OnSlideChanged;
        if (speakerTimer != null) speakerTimer.TimerFinished += OnTimerFinished;
    }

    private void OnDisable()
    {
        if (slideChanger != null) slideChanger.SlideChanged -= OnSlideChanged;
        if (speakerTimer != null) speakerTimer.TimerFinished -= OnTimerFinished;
    }

    private void Update()
    {
        if (Input.GetKeyDown(menuToggleKey) && menuCanvas != null)
        {
            menuCanvas.gameObject.SetActive(!menuCanvas.gameObject.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.T)) ToggleTimer();
        if (Input.GetKeyDown(KeyCode.F)) FinishSession();

        if (sessionStarted && !sessionFinished && speakerTimer != null && speakerTimer.IsRunning)
        {
            sessionElapsed += Time.deltaTime;
        }

        RefreshStats();
    }

    public void StartSession()
    {
        ResolveReferences();
        sessionElapsed = 0f;
        sessionFinished = false;
        sessionStarted = true;
        successfulFinalReactionPlayed = false;
        resultScreenScheduled = false;
        slideChangesAtStart = slideChanger != null ? slideChanger.ManualChangeCount : 0;
        HideResultScreen();

        if (speakerTimer != null)
        {
            speakerTimer.SetDurationMinutes(GetSelectedMinutes());
            speakerTimer.StartTimer();
        }

        if (audience != null)
        {
            audience.StartAmbientReactions();
            audience.TriggerPositiveReaction();
        }

        SetStatus("\u0422\u0440\u0435\u043d\u0438\u0440\u043e\u0432\u043a\u0430 \u043d\u0430\u0447\u0430\u043b\u0430\u0441\u044c");
        ShowTrainingControls();
    }

    public void ToggleTimer()
    {
        if (!sessionStarted)
        {
            StartSession();
            return;
        }

        if (speakerTimer == null) return;

        speakerTimer.ToggleTimer();
        SetStatus(speakerTimer.IsRunning
            ? "\u0422\u0430\u0439\u043c\u0435\u0440 \u0437\u0430\u043f\u0443\u0449\u0435\u043d"
            : "\u0422\u0430\u0439\u043c\u0435\u0440 \u043d\u0430 \u043f\u0430\u0443\u0437\u0435");
    }

    public void FinishSession()
    {
        FinishSession(true);
    }

    public void FinishSession(bool manualFinish)
    {
        if (sessionFinished) return;

        sessionFinished = true;
        if (speakerTimer != null) speakerTimer.PauseTimer();

        if (audience != null)
        {
            audience.StopAmbientReactions();
            if (IsLastSlide() && speakerTimer != null && speakerTimer.RemainingTime > 0f)
            {
                audience.TriggerFinalApplause();
            }
        }

        SetStatus(manualFinish
            ? "\u0422\u0440\u0435\u043d\u0438\u0440\u043e\u0432\u043a\u0430 \u0437\u0430\u0432\u0435\u0440\u0448\u0435\u043d\u0430"
            : "\u0412\u0440\u0435\u043c\u044f \u0432\u044b\u0448\u043b\u043e");
        RefreshStats();
    }

    public void ResetSession()
    {
        sessionElapsed = 0f;
        sessionStarted = false;
        sessionFinished = false;
        successfulFinalReactionPlayed = false;
        resultScreenScheduled = false;
        slideChangesAtStart = slideChanger != null ? slideChanger.ManualChangeCount : 0;
        HideResultScreen();

        if (speakerTimer != null)
        {
            speakerTimer.SetDurationMinutes(GetSelectedMinutes());
        }

        if (slideChanger != null) slideChanger.ResetSlides();
        if (audience != null) audience.StopAmbientReactions();

        SetStatus("\u0413\u043e\u0442\u043e\u0432\u043e \u043a \u0437\u0430\u043f\u0443\u0441\u043a\u0443");
        ShowSetup();
        RefreshStats();
    }

    public void NextSlide()
    {
        if (slideChanger != null) slideChanger.NextSlide();
    }

    public void PreviousSlide()
    {
        if (slideChanger != null) slideChanger.PreviousSlide();
    }

    public void TriggerAudienceReaction()
    {
        if (audience == null) return;

        audience.TriggerPositiveReaction();
        SetStatus("\u0410\u0443\u0434\u0438\u0442\u043e\u0440\u0438\u044f \u0440\u0435\u0430\u0433\u0438\u0440\u0443\u0435\u0442");
    }

    private void ResolveReferences()
    {
        if (slideChanger == null) slideChanger = FindObjectOfType<SlideChanger>();
        if (speakerTimer == null) speakerTimer = FindObjectOfType<SpeakerTimer>();
        if (audience == null) audience = FindObjectOfType<AudienceReactionController>();

        if (audience == null)
        {
            audience = gameObject.AddComponent<AudienceReactionController>();
        }

        if (menuAnchor == null && Camera.main != null)
        {
            menuAnchor = Camera.main.transform;
        }
    }

    private void ImproveSceneLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.72f, 0.72f, 0.72f, 1f);
        RenderSettings.ambientIntensity = 1.25f;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.nearClipPlane = 0.01f;
            mainCamera.farClipPlane = 1000f;
            mainCamera.allowHDR = false;
            mainCamera.allowMSAA = false;
        }

        foreach (Light light in FindObjectsOfType<Light>())
        {
            if (light.type == LightType.Directional)
            {
                light.intensity = 1.15f;
                light.shadows = LightShadows.None;
            }
        }

        if (slideChanger != null)
        {
            Renderer slideRenderer = slideChanger.GetComponent<Renderer>();
            if (slideRenderer != null)
            {
                slideRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                slideRenderer.receiveShadows = false;
            }
        }
    }

    private void CreateMenu()
    {
        if (menuCanvas != null) return;

        GameObject canvasObject = new GameObject("Presentation Menu");
        canvasObject.transform.SetParent(menuAnchor != null ? menuAnchor : transform, false);
        canvasObject.transform.localPosition = new Vector3(0f, -0.15f, 1.8f);
        canvasObject.transform.localRotation = Quaternion.identity;
        canvasObject.transform.localScale = Vector3.one * 0.0015f;

        menuCanvas = canvasObject.AddComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.WorldSpace;
        menuCanvas.GetComponent<RectTransform>().sizeDelta = new Vector2(660f, 470f);

        canvasObject.AddComponent<GraphicRaycaster>();
        canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        Image background = canvasObject.AddComponent<Image>();
        background.color = new Color(0.04f, 0.05f, 0.06f, 0.86f);

        CreateText(canvasObject.transform, "Title", "\u0422\u0440\u0435\u043d\u0430\u0436\u0435\u0440 \u043f\u0443\u0431\u043b\u0438\u0447\u043d\u043e\u0433\u043e \u0432\u044b\u0441\u0442\u0443\u043f\u043b\u0435\u043d\u0438\u044f", 27, new Vector2(0f, 190f), new Vector2(600f, 46f));
        statusText = CreateText(canvasObject.transform, "Status", "\u0413\u043b\u0430\u0432\u043d\u043e\u0435 \u043c\u0435\u043d\u044e", 20, new Vector2(0f, 145f), new Vector2(600f, 36f));
        statsText = CreateText(canvasObject.transform, "Stats", "", 20, new Vector2(0f, 10f), new Vector2(590f, 150f));

        setupPanel = CreatePanel(canvasObject.transform, "Setup Panel");
        CreateButton(setupPanel.transform, "Open Setup", "\u041d\u0430\u0447\u0430\u0442\u044c \u0442\u0440\u0435\u043d\u0438\u0440\u043e\u0432\u043a\u0443", new Vector2(0f, 65f), ShowTimerSetup);
        minuteDropdown = CreateMinuteDropdown(setupPanel.transform, new Vector2(0f, -15f));
        minuteDropdown.gameObject.SetActive(false);
        CreateButton(setupPanel.transform, "Confirm Start", "\u0421\u0442\u0430\u0440\u0442", new Vector2(0f, -88f), StartSession).gameObject.SetActive(false);

        controlsPanel = CreatePanel(canvasObject.transform, "Controls Panel");
        Button timerButton = CreateButton(controlsPanel.transform, "Timer", "\u041f\u0430\u0443\u0437\u0430", new Vector2(-220f, -92f), ToggleTimer);
        timerButtonText = timerButton.GetComponentInChildren<TextMeshProUGUI>();
        CreateButton(controlsPanel.transform, "Finish", "\u0417\u0430\u0432\u0435\u0440\u0448\u0438\u0442\u044c", new Vector2(0f, -92f), FinishSession);
        CreateButton(controlsPanel.transform, "Reset", "\u0421\u0431\u0440\u043e\u0441", new Vector2(220f, -92f), ResetSession);
        CreateButton(controlsPanel.transform, "Prev", "\u041d\u0430\u0437\u0430\u0434", new Vector2(-220f, -152f), PreviousSlide);
        CreateButton(controlsPanel.transform, "Next", "\u0412\u043f\u0435\u0440\u0435\u0434", new Vector2(0f, -152f), NextSlide);
        CreateButton(controlsPanel.transform, "Audience", "\u0420\u0435\u0430\u043a\u0446\u0438\u044f \u0437\u0430\u043b\u0430", new Vector2(220f, -152f), TriggerAudienceReaction);

        ShowSetup();
        RefreshStats();
    }

    private GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(620f, 320f);
        rect.anchoredPosition = new Vector2(0f, -15f);
        return panel;
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(170f, 46f);
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.16f, 0.25f, 0.32f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", label, 20, Vector2.zero, rect.sizeDelta);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private TMP_Dropdown CreateMinuteDropdown(Transform parent, Vector2 anchoredPosition)
    {
        GameObject dropdownObject = new GameObject("Timer Minutes Dropdown");
        dropdownObject.transform.SetParent(parent, false);

        RectTransform rect = dropdownObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300f, 52f);
        rect.anchoredPosition = anchoredPosition;

        Image image = dropdownObject.AddComponent<Image>();
        image.color = new Color(0.12f, 0.17f, 0.2f, 0.98f);

        TMP_Dropdown dropdown = dropdownObject.AddComponent<TMP_Dropdown>();
        TextMeshProUGUI label = CreateText(dropdownObject.transform, "Label", "", 20, Vector2.zero, rect.sizeDelta);
        label.alignment = TextAlignmentOptions.Center;
        dropdown.captionText = label;
        CreateDropdownTemplate(dropdownObject.transform, dropdown);

        dropdown.options = BuildMinuteOptions();
        dropdown.value = Mathf.Max(0, System.Array.IndexOf(trainingMinuteOptions, defaultTrainingMinutes));
        dropdown.RefreshShownValue();
        return dropdown;
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

    private void CreateDropdownTemplate(Transform parent, TMP_Dropdown dropdown)
    {
        GameObject templateObject = new GameObject("Template");
        templateObject.transform.SetParent(parent, false);
        RectTransform templateRect = templateObject.AddComponent<RectTransform>();
        templateRect.sizeDelta = new Vector2(300f, 210f);
        templateRect.anchoredPosition = new Vector2(0f, -132f);

        Image templateImage = templateObject.AddComponent<Image>();
        templateImage.color = new Color(0.08f, 0.1f, 0.12f, 0.98f);

        ScrollRect scrollRect = templateObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        GameObject viewportObject = new GameObject("Viewport");
        viewportObject.transform.SetParent(templateObject.transform, false);
        RectTransform viewportRect = viewportObject.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(6f, 6f);
        viewportRect.offsetMax = new Vector2(-6f, -6f);
        Image viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.15f);
        viewportObject.AddComponent<Mask>().showMaskGraphic = false;

        GameObject contentObject = new GameObject("Content");
        contentObject.transform.SetParent(viewportObject.transform, false);
        RectTransform contentRect = contentObject.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 330f);

        GameObject itemObject = new GameObject("Item");
        itemObject.transform.SetParent(contentObject.transform, false);
        RectTransform itemRect = itemObject.AddComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0f, 1f);
        itemRect.anchorMax = new Vector2(1f, 1f);
        itemRect.pivot = new Vector2(0.5f, 1f);
        itemRect.sizeDelta = new Vector2(0f, 42f);

        Toggle toggle = itemObject.AddComponent<Toggle>();
        Image itemBackground = itemObject.AddComponent<Image>();
        itemBackground.color = new Color(0.14f, 0.21f, 0.25f, 0.98f);
        toggle.targetGraphic = itemBackground;

        TextMeshProUGUI itemLabel = CreateText(itemObject.transform, "Item Label", "", 18, Vector2.zero, itemRect.sizeDelta);
        itemLabel.alignment = TextAlignmentOptions.Center;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        dropdown.template = templateRect;
        dropdown.itemText = itemLabel;
        templateObject.SetActive(false);
    }

    private List<TMP_Dropdown.OptionData> BuildMinuteOptions()
    {
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        if (trainingMinuteOptions == null || trainingMinuteOptions.Length == 0)
        {
            trainingMinuteOptions = new[] { 1, 3, 5, 7, 10, 15, 20 };
        }

        foreach (int minutes in trainingMinuteOptions)
        {
            if (minutes > 0)
            {
                options.Add(new TMP_Dropdown.OptionData($"{minutes} \u043c\u0438\u043d."));
            }
        }

        return options;
    }

    private void ShowTimerSetup()
    {
        if (minuteDropdown != null) minuteDropdown.gameObject.SetActive(true);

        Transform confirm = setupPanel != null ? setupPanel.transform.Find("Confirm Start") : null;
        if (confirm != null) confirm.gameObject.SetActive(true);

        SetStatus("\u0412\u044b\u0431\u0435\u0440\u0438\u0442\u0435 \u0434\u043b\u0438\u0442\u0435\u043b\u044c\u043d\u043e\u0441\u0442\u044c \u0442\u0440\u0435\u043d\u0438\u0440\u043e\u0432\u043a\u0438");
    }

    private void ShowSetup()
    {
        if (setupPanel != null) setupPanel.SetActive(true);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (minuteDropdown != null) minuteDropdown.gameObject.SetActive(false);

        Transform confirm = setupPanel != null ? setupPanel.transform.Find("Confirm Start") : null;
        if (confirm != null) confirm.gameObject.SetActive(false);
    }

    private void ShowTrainingControls()
    {
        if (setupPanel != null) setupPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(true);
    }

    private void RefreshStats()
    {
        if (statsText == null) return;

        int slideIndex = slideChanger != null ? slideChanger.CurrentSlideIndex + 1 : 0;
        int slideCount = slideChanger != null ? slideChanger.SlideCount : 0;
        int changes = slideChanger != null ? Mathf.Max(0, slideChanger.ManualChangeCount - slideChangesAtStart) : 0;
        float remaining = speakerTimer != null ? speakerTimer.RemainingTime : 0f;
        string timerState = speakerTimer != null && speakerTimer.IsRunning ? "\u0438\u0434\u0435\u0442" : "\u043f\u0430\u0443\u0437\u0430";

        statsText.text =
            $"\u0412\u0440\u0435\u043c\u044f \u0432\u044b\u0441\u0442\u0443\u043f\u043b\u0435\u043d\u0438\u044f: {FormatTime(sessionElapsed)}\n" +
            $"\u041e\u0441\u0442\u0430\u043b\u043e\u0441\u044c: {FormatTime(remaining)} ({timerState})\n" +
            $"\u0421\u043b\u0430\u0439\u0434: {slideIndex}/{slideCount}\n" +
            $"\u041f\u0435\u0440\u0435\u043a\u043b\u044e\u0447\u0435\u043d\u0438\u0439: {changes}\n" +
            $"\u0417\u0430\u043b: {(audience != null ? "\u0430\u043a\u0442\u0438\u0432\u0435\u043d" : "\u043d\u0435 \u043d\u0430\u0439\u0434\u0435\u043d")}";

        if (timerButtonText != null && speakerTimer != null)
        {
            timerButtonText.text = speakerTimer.IsRunning ? "\u041f\u0430\u0443\u0437\u0430" : "\u0421\u0442\u0430\u0440\u0442";
        }
    }

    private void SetStatus(string status)
    {
        if (statusText != null) statusText.text = status;
    }

    private void ScheduleResultScreen(ResultScreenMode mode)
    {
        if (resultScreenScheduled) return;

        resultScreenScheduled = true;
        if (resultScreenRoutine != null)
        {
            StopCoroutine(resultScreenRoutine);
        }

        resultScreenRoutine = StartCoroutine(ShowResultScreenAfterDelay(mode));
    }

    private IEnumerator ShowResultScreenAfterDelay(ResultScreenMode mode)
    {
        yield return new WaitForSeconds(ResultScreenDelaySeconds);
        ShowResultScreen(mode);
        resultScreenRoutine = null;
    }

    private void ShowResultScreen(ResultScreenMode mode)
    {
        if (menuCanvas != null) menuCanvas.gameObject.SetActive(false);

        if (resultCanvas != null)
        {
            Destroy(resultCanvas.gameObject);
            resultCanvas = null;
        }

        GameObject canvasObject = new GameObject("Training Result Canvas");
        canvasObject.transform.SetParent(menuAnchor != null ? menuAnchor : transform, false);
        canvasObject.transform.localPosition = new Vector3(0f, -0.1f, 1.65f);
        canvasObject.transform.localRotation = Quaternion.identity;
        canvasObject.transform.localScale = Vector3.one * 0.0016f;

        resultCanvas = canvasObject.AddComponent<Canvas>();
        resultCanvas.renderMode = RenderMode.WorldSpace;
        resultCanvas.worldCamera = Camera.main;
        resultCanvas.GetComponent<RectTransform>().sizeDelta = new Vector2(720f, 430f);

        canvasObject.AddComponent<GraphicRaycaster>();
        canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        Image background = canvasObject.AddComponent<Image>();
        background.color = new Color(0.035f, 0.045f, 0.052f, 0.92f);

        string title = mode == ResultScreenMode.FinishedOnLastSlideInTime
            ? "Тренировка завершена"
            : "Время выступления вышло";

        CreateText(canvasObject.transform, "Result Title", title, 32, new Vector2(0f, 150f), new Vector2(650f, 56f));
        CreateText(canvasObject.transform, "Result Stats", BuildResultStatsText(mode), 24, new Vector2(0f, 25f), new Vector2(640f, 170f));
        CreateButton(canvasObject.transform, "Return To Menu", "В главное меню", new Vector2(0f, -145f), ReturnToMainMenu);
    }

    private string BuildResultStatsText(ResultScreenMode mode)
    {
        float spent = GetSpentTime();
        float remaining = speakerTimer != null ? speakerTimer.RemainingTime : 0f;

        if (mode == ResultScreenMode.TimeExpiredBeforeLastSlide)
        {
            int remainingSlides = 0;
            if (slideChanger != null)
            {
                remainingSlides = Mathf.Max(0, slideChanger.SlideCount - slideChanger.CurrentSlideIndex - 1);
            }

            return
                $"Осталось слайдов: {remainingSlides}\n" +
                $"Затрачено времени: {FormatTime(spent)}";
        }

        return
            $"Затрачено времени: {FormatTime(spent)}\n" +
            $"Осталось времени: {FormatTime(remaining)}";
    }

    private void HideResultScreen()
    {
        if (resultScreenRoutine != null)
        {
            StopCoroutine(resultScreenRoutine);
            resultScreenRoutine = null;
        }

        if (resultCanvas != null)
        {
            Destroy(resultCanvas.gameObject);
            resultCanvas = null;
        }
    }

    private float GetSpentTime()
    {
        if (speakerTimer != null)
        {
            return Mathf.Max(sessionElapsed, speakerTimer.ElapsedTime);
        }

        return sessionElapsed;
    }

    public void ReturnToMainMenu()
    {
        if (!string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void OnSlideChanged(int index, int count)
    {
        bool finalSlideReached = sessionStarted && index == count - 1 && speakerTimer != null;
        if (!finalSlideReached) return;

        speakerTimer.PauseTimer();
        sessionFinished = true;

        if (successfulFinalReactionPlayed)
        {
            RefreshStats();
            return;
        }

        if (speakerTimer.RemainingTime <= 0f)
        {
            SetStatus("\u0424\u0438\u043d\u0430\u043b\u044c\u043d\u044b\u0439 \u0441\u043b\u0430\u0439\u0434: \u0442\u0430\u0439\u043c\u0435\u0440 \u043e\u0441\u0442\u0430\u043d\u043e\u0432\u043b\u0435\u043d");
            RefreshStats();
            return;
        }

        successfulFinalReactionPlayed = true;
        if (audience != null)
        {
            audience.StopAmbientReactions();
            audience.TriggerFinalApplause();
        }

        SetStatus("\u0424\u0438\u043d\u0430\u043b\u044c\u043d\u044b\u0439 \u0441\u043b\u0430\u0439\u0434: \u0437\u0430\u043b \u0430\u043f\u043b\u043e\u0434\u0438\u0440\u0443\u0435\u0442");
        RefreshStats();
        ScheduleResultScreen(ResultScreenMode.FinishedOnLastSlideInTime);
    }

    private void OnTimerFinished()
    {
        if (!IsLastSlide())
        {
            if (audience != null)
            {
                audience.StopAmbientReactions();
                audience.TriggerNegativeReaction();
            }

            SetStatus("\u0412\u0440\u0435\u043c\u044f \u0432\u044b\u0448\u043b\u043e: \u0437\u0430\u043b \u043d\u0435\u0434\u043e\u0432\u043e\u043b\u0435\u043d");
            sessionFinished = true;
            RefreshStats();
            ScheduleResultScreen(ResultScreenMode.TimeExpiredBeforeLastSlide);
            return;
        }

        FinishSession(false);
    }

    private bool IsLastSlide()
    {
        return slideChanger != null && slideChanger.SlideCount > 0 && slideChanger.CurrentSlideIndex == slideChanger.SlideCount - 1;
    }

    private int GetSelectedMinutes()
    {
        if (minuteDropdown == null || trainingMinuteOptions == null || trainingMinuteOptions.Length == 0)
        {
            return Mathf.Max(1, PlayerPrefs.GetInt(DurationMinutesKey, defaultTrainingMinutes));
        }

        int index = Mathf.Clamp(minuteDropdown.value, 0, trainingMinuteOptions.Length - 1);
        return Mathf.Max(1, trainingMinuteOptions[index]);
    }

    private void EnsureEventSystem()
    {
        EventSystem existing = FindObjectOfType<EventSystem>();
        if (existing != null)
        {
            if (existing.GetComponent<StandaloneInputModule>() == null)
            {
                existing.gameObject.AddComponent<StandaloneInputModule>();
            }

            if (existing.GetComponent<XRUIInputModule>() == null)
            {
                existing.gameObject.AddComponent<XRUIInputModule>();
            }
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
        eventSystem.AddComponent<XRUIInputModule>();
    }

    private void EnsureVrMovement()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        Transform root = mainCamera.transform;
        while (root.parent != null && root.parent.name != "XR Origin (XR Rig)")
        {
            root = root.parent;
        }

        if (root.parent != null && root.parent.name == "XR Origin (XR Rig)")
        {
            root = root.parent;
        }

        VRThumbstickMovement movement = root.GetComponent<VRThumbstickMovement>();
        if (movement == null)
        {
            movement = root.gameObject.AddComponent<VRThumbstickMovement>();
        }

        movement.xrRoot = root;
        movement.head = mainCamera.transform;
    }

    private string FormatTime(float seconds)
    {
        int rounded = Mathf.Max(0, Mathf.CeilToInt(seconds));
        return $"{rounded / 60:00}:{rounded % 60:00}";
    }
}
