using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

#if UNITY_ANDROID
using System;
using VIVE.OpenXR;
using VIVE.OpenXR.DisplayRefreshRate;
#endif

public class VRRuntimeOptimizer : MonoBehaviour
{
    private static readonly bool EnableRuntimeOptimizer = false;

    public float androidEyeTextureScale = 0.72f;
    public int androidTargetFrameRate = 72;
    public int maxAnimatedAudienceOnAndroid = 24;
    public bool disableRealtimeShadows = true;
    public bool optimizeAudienceAnimators = true;
    public bool applyViveFoveation = true;

    private static bool created;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateOnSceneLoad()
    {
        if (!EnableRuntimeOptimizer) return;
        if (created) return;

        GameObject optimizerObject = new GameObject("VR Runtime Optimizer");
        DontDestroyOnLoad(optimizerObject);
        optimizerObject.AddComponent<VRRuntimeOptimizer>();
        created = true;
    }

    private void Awake()
    {
        ApplyGlobalSettings();
    }

    private void OnEnable()
    {
        StartCoroutine(ApplyAfterSceneObjectsLoaded());
    }

    private IEnumerator ApplyAfterSceneObjectsLoaded()
    {
        yield return null;
        ApplyCameraSettings();
        ApplyRendererSettings();
        ApplyAudienceSettings();
        ApplyHeadsetFeatures();
    }

    private void ApplyGlobalSettings()
    {
        Application.targetFrameRate = androidTargetFrameRate;
        QualitySettings.vSyncCount = 0;
        QualitySettings.antiAliasing = 0;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.softParticles = false;
        QualitySettings.skinWeights = SkinWeights.TwoBones;
        Time.fixedDeltaTime = 1f / androidTargetFrameRate;

#if UNITY_ANDROID
        QualitySettings.SetQualityLevel(1, true);
        XRSettings.eyeTextureResolutionScale = androidEyeTextureScale;
#endif
    }

    private void ApplyCameraSettings()
    {
        Camera mainCamera = Camera.main;
        foreach (Camera camera in FindObjectsOfType<Camera>(true))
        {
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = true;
            camera.stereoTargetEye = StereoTargetEyeMask.Both;

            if (mainCamera != null && camera != mainCamera && XRSettings.enabled)
            {
                camera.enabled = false;
            }
        }
    }

    private void ApplyRendererSettings()
    {
        if (!disableRealtimeShadows) return;

        foreach (Light light in FindObjectsOfType<Light>(true))
        {
            light.shadows = LightShadows.None;
            if (light.type == LightType.Directional)
            {
                light.intensity = Mathf.Min(light.intensity, 1.05f);
            }
        }

        foreach (Renderer renderer in FindObjectsOfType<Renderer>(true))
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private void ApplyAudienceSettings()
    {
        if (!optimizeAudienceAnimators) return;

        Animator[] animators = FindObjectsOfType<Animator>(true);
        int activeAudienceAnimators = 0;

        foreach (Animator animator in animators)
        {
            if (animator == null || animator.runtimeAnimatorController == null) continue;

            string objectName = animator.gameObject.name.ToLowerInvariant();
            bool isAudienceMember = objectName.StartsWith("char_") || objectName.Contains("audience");

            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

#if UNITY_ANDROID
            if (isAudienceMember)
            {
                activeAudienceAnimators++;
                if (activeAudienceAnimators > maxAnimatedAudienceOnAndroid)
                {
                    animator.enabled = false;
                }
            }
#endif
        }

        foreach (SkinnedMeshRenderer renderer in FindObjectsOfType<SkinnedMeshRenderer>(true))
        {
            renderer.updateWhenOffscreen = false;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.quality = SkinQuality.Bone2;
        }
    }

    private void ApplyHeadsetFeatures()
    {
#if UNITY_ANDROID
        if (applyViveFoveation)
        {
            TryApplyViveFoveation();
        }

        try
        {
            XR_FB_display_refresh_rate.RequestDisplayRefreshRate(androidTargetFrameRate);
        }
        catch (Exception exception)
        {
            Debug.Log($"Display refresh rate request was skipped: {exception.Message}");
        }
#endif
    }

#if UNITY_ANDROID
    private void TryApplyViveFoveation()
    {
        try
        {
            XrFoveationConfigurationHTC[] configs =
            {
                new XrFoveationConfigurationHTC
                {
                    level = XrFoveationLevelHTC.XR_FOVEATION_LEVEL_HIGH_HTC,
                    clearFovDegree = 38f,
                    focalCenterOffset = new XrVector2f { x = 0f, y = 0f }
                },
                new XrFoveationConfigurationHTC
                {
                    level = XrFoveationLevelHTC.XR_FOVEATION_LEVEL_HIGH_HTC,
                    clearFovDegree = 38f,
                    focalCenterOffset = new XrVector2f { x = 0f, y = 0f }
                }
            };

            ViveFoveation.ApplyFoveationHTC(
                XrFoveationModeHTC.XR_FOVEATION_MODE_FIXED_HTC,
                (uint)configs.Length,
                configs);
        }
        catch (Exception exception)
        {
            Debug.Log($"VIVE foveation was skipped: {exception.Message}");
        }
    }
#endif
}
