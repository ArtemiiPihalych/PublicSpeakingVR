using UnityEngine;
using TMPro;
using System;

public class SpeakerTimer : MonoBehaviour
{
    [Tooltip("Presentation time in seconds. 300 = 5 minutes.")]
    public float totalTime = 300f;

    [Tooltip("Text object that shows the remaining time.")]
    public TextMeshPro timerText;
    public bool startAutomatically = true;
    public float warningTime = 60f;
    public Color normalColor = Color.white;
    public Color warningColor = Color.red;

    public event Action TimerStarted;
    public event Action TimerPaused;
    public event Action TimerReset;
    public event Action TimerFinished;

    private float currentTime;
    private bool isRunning;
    private bool finishEventSent;

    public float RemainingTime => currentTime;
    public bool IsRunning => isRunning;
    public float ElapsedTime => Mathf.Max(0f, totalTime - currentTime);
    public float Progress01 => totalTime <= 0f ? 1f : Mathf.Clamp01(ElapsedTime / totalTime);

    private void Start()
    {
        ResetTimer();
        if (startAutomatically)
        {
            StartTimer();
        }
    }

    private void Update()
    {
        if (!isRunning) return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            isRunning = false;
            if (!finishEventSent)
            {
                finishEventSent = true;
                TimerFinished?.Invoke();
            }
        }

        UpdateText();
    }

    public void StartTimer()
    {
        if (currentTime <= 0f)
        {
            ResetTimer();
        }

        isRunning = true;
        TimerStarted?.Invoke();
    }

    public void PauseTimer()
    {
        isRunning = false;
        TimerPaused?.Invoke();
    }

    public void ToggleTimer()
    {
        if (isRunning) PauseTimer();
        else StartTimer();
    }

    public void ResetTimer()
    {
        totalTime = Mathf.Max(0f, totalTime);
        currentTime = totalTime;
        isRunning = false;
        finishEventSent = false;
        UpdateText();
        TimerReset?.Invoke();
    }

    public void AddTime(float seconds)
    {
        currentTime = Mathf.Max(0f, currentTime + seconds);
        UpdateText();
    }

    private void UpdateText()
    {
        int timeLeft = Mathf.CeilToInt(currentTime);
        int minutes = timeLeft / 60;
        int seconds = timeLeft % 60;

        if (timerText != null)
        {
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            timerText.color = currentTime <= warningTime ? warningColor : normalColor;
        }
    }

    private void OnValidate()
    {
        totalTime = Mathf.Max(0f, totalTime);
        warningTime = Mathf.Max(0f, warningTime);
    }
}
