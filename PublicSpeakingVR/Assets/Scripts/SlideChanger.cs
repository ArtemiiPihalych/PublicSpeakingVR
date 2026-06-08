using UnityEngine;
using System;

public class SlideChanger : MonoBehaviour
{
    [Tooltip("Slide textures in presentation order.")]
    public Texture[] slides;

    public event Action<int, int> SlideChanged;

    private int currentSlideIndex;
    private Renderer myRenderer;
    private int manualChangeCount;

    public int CurrentSlideIndex => currentSlideIndex;
    public int SlideCount => slides != null ? slides.Length : 0;
    public int ManualChangeCount => manualChangeCount;

    private void Start()
    {
        myRenderer = GetComponent<Renderer>();

        if (myRenderer == null)
        {
            Debug.LogWarning($"{nameof(SlideChanger)} needs a Renderer on {gameObject.name}.", this);
        }

        UpdateSlide();
    }

    public void NextSlide()
    {
        SetSlide(currentSlideIndex + 1, true);
    }

    public void PreviousSlide()
    {
        SetSlide(currentSlideIndex - 1, true);
    }

    public void ResetSlides()
    {
        SetSlide(0, false);
    }

    public void SetSlide(int index)
    {
        SetSlide(index, false);
    }

    private void SetSlide(int index, bool countAsManualChange)
    {
        if (slides == null || slides.Length == 0) return;

        currentSlideIndex = (index % slides.Length + slides.Length) % slides.Length;
        if (countAsManualChange)
        {
            manualChangeCount++;
        }

        UpdateSlide();
    }

    private void UpdateSlide()
    {
        if (slides != null && slides.Length > 0 && myRenderer != null)
        {
            myRenderer.material.mainTexture = slides[currentSlideIndex];
            SlideChanged?.Invoke(currentSlideIndex, slides.Length);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            NextSlide();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            PreviousSlide();
        }
        else if (Input.GetKeyDown(KeyCode.Home) || Input.GetKeyDown(KeyCode.R))
        {
            ResetSlides();
        }
    }
}
