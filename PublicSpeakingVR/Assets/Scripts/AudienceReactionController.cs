using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudienceReactionController : MonoBehaviour
{
    [Tooltip("Audience animators. If empty, the script will find all scene animators except UI/camera helpers.")]
    public Animator[] audienceAnimators;
    public float ambientReactionInterval = 6f;
    public float attentionDropSpeed = 0.35f;
    public float positiveReactionSpeed = 1.8f;
    public float reactionDuration = 2.5f;
    public AudioClip applauseClip;
    public AudioClip negativeReactionClip;
    public float reactionVolume = 1f;
    public KeyCode testApplauseKey = KeyCode.P;
    public KeyCode testNegativeKey = KeyCode.O;

    private readonly Dictionary<Animator, float> defaultSpeeds = new Dictionary<Animator, float>();
    private Coroutine ambientRoutine;
    private AudioSource audioSource;
    private Coroutine pendingPlayback;

    private void Awake()
    {
        CollectAudience();
        CacheDefaultSpeeds();
        RandomizeIdleOffsets();
        EnsureAudio();
    }

    private void Update()
    {
        if (Input.GetKeyDown(testApplauseKey))
        {
            TriggerFinalApplause();
        }

        if (Input.GetKeyDown(testNegativeKey))
        {
            TriggerNegativeReaction();
        }
    }

    public void CollectAudience()
    {
        if (audienceAnimators != null && audienceAnimators.Length > 0) return;

        List<Animator> found = new List<Animator>();
        foreach (Animator animator in FindObjectsOfType<Animator>())
        {
            if (animator == null || animator.runtimeAnimatorController == null) continue;
            string objectName = animator.gameObject.name.ToLowerInvariant();
            if (objectName.Contains("controller") || objectName.Contains("camera")) continue;
            found.Add(animator);
        }

        audienceAnimators = found.ToArray();
    }

    public void StartAmbientReactions()
    {
        if (ambientRoutine != null) StopCoroutine(ambientRoutine);
        ambientRoutine = StartCoroutine(AmbientReactionLoop());
    }

    public void StopAmbientReactions()
    {
        if (ambientRoutine != null)
        {
            StopCoroutine(ambientRoutine);
            ambientRoutine = null;
        }

        RestoreDefaultSpeeds();
    }

    public void TriggerPositiveReaction()
    {
        StartCoroutine(TemporarySpeedWave(positiveReactionSpeed, reactionDuration, 0.08f));
    }

    public void TriggerAttentionDrop()
    {
        StartCoroutine(TemporarySpeedWave(attentionDropSpeed, reactionDuration, 0.04f));
    }

    public void TriggerFinalApplause()
    {
        PlayReactionSound(applauseClip);
        StartCoroutine(TemporarySpeedWave(positiveReactionSpeed + 0.6f, reactionDuration + 2f, 0.05f));
    }

    public void TriggerNegativeReaction()
    {
        PlayReactionSound(negativeReactionClip);
        StartCoroutine(TemporarySpeedWave(attentionDropSpeed, reactionDuration + 1.5f, 0.04f));
    }

    private IEnumerator AmbientReactionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(ambientReactionInterval * 0.7f, ambientReactionInterval * 1.3f));

            if (Random.value > 0.35f)
            {
                TriggerPositiveReaction();
            }
            else
            {
                TriggerAttentionDrop();
            }
        }
    }

    private IEnumerator TemporarySpeedWave(float targetSpeed, float duration, float perMemberDelay)
    {
        if (audienceAnimators == null || audienceAnimators.Length == 0) yield break;

        for (int i = 0; i < audienceAnimators.Length; i++)
        {
            Animator animator = audienceAnimators[i];
            if (animator == null) continue;

            animator.speed = targetSpeed * Random.Range(0.85f, 1.15f);
            yield return new WaitForSeconds(perMemberDelay);
        }

        yield return new WaitForSeconds(duration);
        RestoreDefaultSpeeds();
    }

    private void CacheDefaultSpeeds()
    {
        defaultSpeeds.Clear();
        if (audienceAnimators == null) return;

        foreach (Animator animator in audienceAnimators)
        {
            if (animator == null || defaultSpeeds.ContainsKey(animator)) continue;
            defaultSpeeds.Add(animator, animator.speed <= 0f ? 1f : animator.speed);
        }
    }

    private void RestoreDefaultSpeeds()
    {
        foreach (KeyValuePair<Animator, float> pair in defaultSpeeds)
        {
            if (pair.Key != null)
            {
                pair.Key.speed = pair.Value;
            }
        }
    }

    private void RandomizeIdleOffsets()
    {
        if (audienceAnimators == null) return;

        foreach (Animator animator in audienceAnimators)
        {
            if (animator == null) continue;
            animator.speed = Random.Range(0.85f, 1.15f);
            animator.Play(0, 0, Random.value);
        }

        CacheDefaultSpeeds();
    }

    private void EnsureAudio()
    {
        Transform audioHost = Camera.main != null ? Camera.main.transform : transform;
        audioSource = audioHost.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = audioHost.gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.dopplerLevel = 0f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 500f;
        audioSource.volume = reactionVolume;
        audioSource.priority = 32;
        audioSource.mute = false;
        audioSource.ignoreListenerPause = true;
        audioSource.bypassEffects = true;
        audioSource.bypassListenerEffects = true;
        audioSource.bypassReverbZones = true;
        AudioListener.volume = 1f;
        AudioListener.pause = false;

        if (applauseClip == null)
        {
            applauseClip = Resources.Load<AudioClip>("Audio/audience_applause") ?? CreateApplauseClip();
        }

        if (negativeReactionClip == null)
        {
            negativeReactionClip = Resources.Load<AudioClip>("Audio/audience_whistle") ?? CreateNegativeReactionClip();
        }
    }

    private void PlayReactionSound(AudioClip clip)
    {
        EnsureAudio();
        if (clip != null)
        {
            if (pendingPlayback != null)
            {
                StopCoroutine(pendingPlayback);
            }

            pendingPlayback = StartCoroutine(PlayWhenLoaded(clip));
        }
        else
        {
            Debug.LogWarning("Audience reaction sound clip is missing.");
        }
    }

    private IEnumerator PlayWhenLoaded(AudioClip clip)
    {
        if (clip.loadState == AudioDataLoadState.Unloaded)
        {
            clip.LoadAudioData();
        }

        float timeoutAt = Time.realtimeSinceStartup + 2f;
        while (clip.loadState == AudioDataLoadState.Loading && Time.realtimeSinceStartup < timeoutAt)
        {
            yield return null;
        }

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogWarning($"Audience reaction sound failed to load: {clip.name}, state={clip.loadState}");
            pendingPlayback = null;
            yield break;
        }

        audioSource.Stop();
        audioSource.clip = null;
        audioSource.volume = reactionVolume;
        audioSource.PlayOneShot(clip, reactionVolume);
        Debug.Log($"Audience reaction sound: {clip.name}, length={clip.length:0.00}, source={audioSource.gameObject.name}, listenerVolume={AudioListener.volume:0.00}, loadState={clip.loadState}, isPlaying={audioSource.isPlaying}");
        pendingPlayback = null;
    }

    private AudioClip CreateApplauseClip()
    {
        const int frequency = 44100;
        const float duration = 3.2f;
        int samples = Mathf.CeilToInt(frequency * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)frequency;
            float clapPulse = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 95f)), 18f);
            float noise = Random.Range(-1f, 1f);
            float envelope = Mathf.Clamp01(t / 0.35f) * Mathf.Clamp01((duration - t) / 0.8f);
            data[i] = noise * clapPulse * envelope * 0.55f;
        }

        AudioClip clip = AudioClip.Create("Generated Audience Applause", samples, 1, frequency, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip CreateNegativeReactionClip()
    {
        const int frequency = 44100;
        const float duration = 2.6f;
        int samples = Mathf.CeilToInt(frequency * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)frequency;
            float whistle = Mathf.Sin(2f * Mathf.PI * (1350f + Mathf.Sin(t * 11f) * 170f) * t);
            float murmur = Random.Range(-0.25f, 0.25f) * Mathf.Sin(t * 16f);
            float envelope = Mathf.Clamp01(t / 0.2f) * Mathf.Clamp01((duration - t) / 0.45f);
            data[i] = (whistle * 0.35f + murmur) * envelope;
        }

        AudioClip clip = AudioClip.Create("Generated Audience Whistle", samples, 1, frequency, false);
        clip.SetData(data, 0);
        return clip;
    }
}
