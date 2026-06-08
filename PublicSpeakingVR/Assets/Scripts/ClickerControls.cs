using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

[RequireComponent(typeof(XRGrabInteractable))]
public class ClickerControls : MonoBehaviour
{
    public PresentationSessionManager sessionManager;
    public bool createButtonPanel = true;
    public bool useActivateForNextSlide = false;
    public Vector3 panelLocalPosition = new Vector3(0f, 0.055f, 0.02f);
    public Vector3 panelLocalEulerAngles = new Vector3(70f, 0f, 0f);

    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (sessionManager == null) sessionManager = FindObjectOfType<PresentationSessionManager>();

        if (createButtonPanel)
        {
            CreateButtonPanel();
        }
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.activated.AddListener(OnActivated);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.activated.RemoveListener(OnActivated);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleTimer();
        }
    }

    public void NextSlide()
    {
        if (sessionManager != null) sessionManager.NextSlide();
    }

    public void ToggleTimer()
    {
        if (sessionManager != null) sessionManager.ToggleTimer();
    }

    public void FinishSession()
    {
        if (sessionManager != null) sessionManager.FinishSession();
    }

    public void AudienceReaction()
    {
        if (sessionManager != null) sessionManager.TriggerAudienceReaction();
    }

    private void OnActivated(ActivateEventArgs args)
    {
        if (useActivateForNextSlide)
        {
            NextSlide();
        }
    }

    private void CreateButtonPanel()
    {
        if (transform.Find("Clicker Button Panel") != null) return;

        GameObject canvasObject = new GameObject("Clicker Button Panel");
        canvasObject.transform.SetParent(transform, false);
        canvasObject.transform.localPosition = panelLocalPosition;
        canvasObject.transform.localEulerAngles = panelLocalEulerAngles;
        canvasObject.transform.localScale = Vector3.one * 0.00055f;

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(360f, 230f);

        canvasObject.AddComponent<GraphicRaycaster>();
        canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        Image background = canvasObject.AddComponent<Image>();
        background.color = new Color(0.02f, 0.025f, 0.03f, 0.92f);

        CreateButton(canvasObject.transform, "Next", "Next", new Vector2(0f, 68f), NextSlide);
        CreateButton(canvasObject.transform, "Timer", "Start/Pause", new Vector2(0f, 16f), ToggleTimer);
        CreateButton(canvasObject.transform, "Finish", "Finish", new Vector2(0f, -36f), FinishSession);
        CreateButton(canvasObject.transform, "Audience", "Audience", new Vector2(0f, -88f), AudienceReaction);
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300f, 42f);
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.28f, 0.34f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        GameObject textObject = new GameObject("Label");
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.sizeDelta = rect.sizeDelta;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 19;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        return button;
    }
}
