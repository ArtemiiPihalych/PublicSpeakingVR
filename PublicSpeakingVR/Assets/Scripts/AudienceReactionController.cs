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

    private readonly Dictionary<Animator, float> defaultSpeeds = new Dictionary<Animator, float>();
    private Coroutine ambientRoutine;

    private void Awake()
    {
        CollectAudience();
        CacheDefaultSpeeds();
        RandomizeIdleOffsets();
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
        StartCoroutine(TemporarySpeedWave(positiveReactionSpeed + 0.6f, reactionDuration + 2f, 0.05f));
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
}
