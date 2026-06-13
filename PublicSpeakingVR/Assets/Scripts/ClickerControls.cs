using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class ClickerControls : MonoBehaviour
{
    public PresentationSessionManager sessionManager;
    public bool createButtonPanel = true;
    public bool useActivateForNextSlide = false;
    public Vector3 panelLocalPosition = new Vector3(0f, 0.055f, 0.02f);
    public Vector3 panelLocalEulerAngles = new Vector3(70f, 0f, 0f);
    public bool protectFromDeskFall = true;
    public bool dockOnStandWhenReleased = true;
    public bool forceVisibleMaterial = true;
    public Color visibleClickerColor = new Color(0.02f, 0.08f, 0.16f, 1f);
    public Vector3 standRestPosition = new Vector3(-1.64f, 1.72f, 9.12f);
    public Vector3 standRestEulerAngles = new Vector3(0f, -31.946f, 0f);

    private XRGrabInteractable grabInteractable;
    private Rigidbody clickerRigidbody;
    private Vector3 safePosition;
    private Quaternion safeRotation;
    private float nextVisibilityCheckTime;
    private bool isHeld;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        clickerRigidbody = GetComponent<Rigidbody>();
        if (sessionManager == null) sessionManager = FindObjectOfType<PresentationSessionManager>();

        EnsureVisibleAndInteractable();
        ConfigurePhysics();
        CacheSafePose();

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
            grabInteractable.selectEntered.AddListener(OnSelectEntered);
            grabInteractable.selectExited.AddListener(OnSelectExited);
        }

        EnsureVisibleAndInteractable();
        if (!isHeld)
        {
            DockOnStand();
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.activated.RemoveListener(OnActivated);
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }
    }

    private void Update()
    {
        if (Time.time >= nextVisibilityCheckTime)
        {
            EnsureVisibleAndInteractable();
            nextVisibilityCheckTime = Time.time + 0.5f;
        }

        PreventDeskFall();

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

    private void ConfigurePhysics()
    {
        if (clickerRigidbody == null) return;

        clickerRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        clickerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        clickerRigidbody.maxDepenetrationVelocity = 2f;
        clickerRigidbody.sleepThreshold = 0.001f;
        DockOnStand();
    }

    private void EnsureVisibleAndInteractable()
    {
        gameObject.SetActive(true);

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = true;
            if (forceVisibleMaterial)
            {
                ApplyVisibleMaterial(renderer);
            }
        }

        foreach (Collider collider in GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = true;
            collider.isTrigger = false;
        }

        if (grabInteractable != null)
        {
            grabInteractable.enabled = true;
        }

        if (clickerRigidbody != null && !isHeld)
        {
            clickerRigidbody.useGravity = false;
            clickerRigidbody.isKinematic = true;
        }
    }

    private void ApplyVisibleMaterial(Renderer renderer)
    {
        foreach (Material material in renderer.materials)
        {
            if (material != null && material.HasProperty("_Color"))
            {
                material.color = visibleClickerColor;
            }
        }
    }

    private void CacheSafePose()
    {
        safePosition = standRestPosition;
        safeRotation = Quaternion.Euler(standRestEulerAngles);
    }

    private void PreventDeskFall()
    {
        if (!protectFromDeskFall || clickerRigidbody == null || isHeld) return;

        float distanceToStand = Vector3.Distance(transform.position, safePosition);
        if (dockOnStandWhenReleased && distanceToStand > 0.025f)
        {
            DockOnStand();
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        isHeld = true;
        if (clickerRigidbody == null) return;

        clickerRigidbody.isKinematic = false;
        clickerRigidbody.useGravity = false;
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        isHeld = false;
        DockOnStand();
    }

    private void DockOnStand()
    {
        if (!dockOnStandWhenReleased || clickerRigidbody == null) return;

        safePosition = standRestPosition;
        safeRotation = Quaternion.Euler(standRestEulerAngles);
        clickerRigidbody.isKinematic = false;
        clickerRigidbody.velocity = Vector3.zero;
        clickerRigidbody.angularVelocity = Vector3.zero;
        clickerRigidbody.useGravity = false;
        clickerRigidbody.isKinematic = true;
        transform.SetPositionAndRotation(safePosition, safeRotation);
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

        CreateButton(canvasObject.transform, "Next", "\u0421\u043b\u0435\u0434\u0443\u044e\u0449\u0438\u0439 \u0441\u043b\u0430\u0439\u0434", new Vector2(0f, 68f), NextSlide);
        CreateButton(canvasObject.transform, "Timer", "\u0421\u0442\u0430\u0440\u0442/\u043f\u0430\u0443\u0437\u0430", new Vector2(0f, 16f), ToggleTimer);
        CreateButton(canvasObject.transform, "Finish", "\u0417\u0430\u0432\u0435\u0440\u0448\u0438\u0442\u044c", new Vector2(0f, -36f), FinishSession);
        CreateButton(canvasObject.transform, "Audience", "\u0420\u0435\u0430\u043a\u0446\u0438\u044f \u0437\u0430\u043b\u0430", new Vector2(0f, -88f), AudienceReaction);
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
