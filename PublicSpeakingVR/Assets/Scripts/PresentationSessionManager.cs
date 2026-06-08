using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class PresentationSessionManager : MonoBehaviour
{
    public SlideChanger slideChanger;
    public SpeakerTimer speakerTimer;
    public AudienceReactionController audience;
    public Transform menuAnchor;
    public bool createMenuOnStart = true;
    public KeyCode menuToggleKey = KeyCode.M;

    private Canvas menuCanvas;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI statsText;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI timerButtonText;
    private float sessionElapsed;
    private int slideChangesAtStart;
    private bool sessionStarted;
    private bool sessionFinished;

    private void Awake()
    {
        ResolveReferences();
        EnsureEventSystem();

        if (createMenuOnStart)
        {
            CreateMenu();
        }
    }

    private void OnEnable()
    {
        if (slideChanger != null)
        {
            slideChanger.SlideChanged += OnSlideChanged;
        }

        if (speakerTimer != null)
        {
            speakerTimer.TimerFinished += FinishSession;
        }
    }

    private void OnDisable()
    {
        if (slideChanger != null)
        {
            slideChanger.SlideChanged -= OnSlideChanged;
        }

        if (speakerTimer != null)
        {
            speakerTimer.TimerFinished -= FinishSession;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(menuToggleKey) && menuCanvas != null)
        {
            menuCanvas.gameObject.SetActive(!menuCanvas.gameObject.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleTimer();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            FinishSession();
        }

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
        slideChangesAtStart = slideChanger != null ? slideChanger.ManualChangeCount : 0;

        if (speakerTimer != null)
        {
            speakerTimer.ResetTimer();
            speakerTimer.StartTimer();
        }

        if (audience != null)
        {
            audience.StartAmbientReactions();
            audience.TriggerPositiveReaction();
        }

        SetStatus("Training started");
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
        SetStatus(speakerTimer.IsRunning ? "Timer running" : "Timer paused");
    }

    public void FinishSession()
    {
        if (sessionFinished) return;

        sessionFinished = true;
        if (speakerTimer != null)
        {
            speakerTimer.PauseTimer();
        }

        if (audience != null)
        {
            audience.StopAmbientReactions();
            audience.TriggerFinalApplause();
        }

        SetStatus("Training finished");
        RefreshStats();
    }

    public void ResetSession()
    {
        sessionElapsed = 0f;
        sessionStarted = false;
        sessionFinished = false;
        slideChangesAtStart = slideChanger != null ? slideChanger.ManualChangeCount : 0;

        if (speakerTimer != null)
        {
            speakerTimer.ResetTimer();
        }

        if (slideChanger != null)
        {
            slideChanger.ResetSlides();
        }

        if (audience != null)
        {
            audience.StopAmbientReactions();
        }

        SetStatus("Ready");
        RefreshStats();
    }

    public void NextSlide()
    {
        if (slideChanger != null)
        {
            slideChanger.NextSlide();
        }
    }

    public void PreviousSlide()
    {
        if (slideChanger != null)
        {
            slideChanger.PreviousSlide();
        }
    }

    public void TriggerAudienceReaction()
    {
        if (audience != null)
        {
            audience.TriggerPositiveReaction();
            SetStatus("Audience reacted");
        }
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
        RectTransform canvasRect = menuCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(620f, 430f);

        canvasObject.AddComponent<GraphicRaycaster>();
        canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        Image background = canvasObject.AddComponent<Image>();
        background.color = new Color(0.04f, 0.05f, 0.06f, 0.86f);

        titleText = CreateText(canvasObject.transform, "Title", "VR Presentation Trainer", 28, new Vector2(0f, 175f), new Vector2(560f, 46f));
        statusText = CreateText(canvasObject.transform, "Status", "Ready", 20, new Vector2(0f, 128f), new Vector2(560f, 36f));
        statsText = CreateText(canvasObject.transform, "Stats", "", 20, new Vector2(0f, 18f), new Vector2(560f, 150f));

        CreateButton(canvasObject.transform, "Start", "Start", new Vector2(-210f, -105f), StartSession);
        Button timerButton = CreateButton(canvasObject.transform, "Timer", "Pause", new Vector2(0f, -105f), ToggleTimer);
        timerButtonText = timerButton.GetComponentInChildren<TextMeshProUGUI>();
        CreateButton(canvasObject.transform, "Finish", "Finish", new Vector2(210f, -105f), FinishSession);
        CreateButton(canvasObject.transform, "Prev", "Prev", new Vector2(-210f, -165f), PreviousSlide);
        CreateButton(canvasObject.transform, "Next", "Next", new Vector2(0f, -165f), NextSlide);
        CreateButton(canvasObject.transform, "Reset", "Reset", new Vector2(210f, -165f), ResetSession);

        RefreshStats();
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

    private void RefreshStats()
    {
        if (statsText == null) return;

        int slideIndex = slideChanger != null ? slideChanger.CurrentSlideIndex + 1 : 0;
        int slideCount = slideChanger != null ? slideChanger.SlideCount : 0;
        int changes = slideChanger != null ? Mathf.Max(0, slideChanger.ManualChangeCount - slideChangesAtStart) : 0;
        float remaining = speakerTimer != null ? speakerTimer.RemainingTime : 0f;
        string timerState = speakerTimer != null && speakerTimer.IsRunning ? "running" : "paused";

        statsText.text =
            $"Time: {FormatTime(sessionElapsed)}\n" +
            $"Remaining: {FormatTime(remaining)} ({timerState})\n" +
            $"Slide: {slideIndex}/{slideCount}\n" +
            $"Slide changes: {changes}\n" +
            $"Audience: {(audience != null ? "active" : "not found")}";

        if (timerButtonText != null && speakerTimer != null)
        {
            timerButtonText.text = speakerTimer.IsRunning ? "Pause" : "Start";
        }
    }

    private void SetStatus(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }
    }

    private void OnSlideChanged(int index, int count)
    {
        if (sessionStarted && audience != null && index == count - 1)
        {
            audience.TriggerPositiveReaction();
        }
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private string FormatTime(float seconds)
    {
        int rounded = Mathf.Max(0, Mathf.CeilToInt(seconds));
        return $"{rounded / 60:00}:{rounded % 60:00}";
    }
}
