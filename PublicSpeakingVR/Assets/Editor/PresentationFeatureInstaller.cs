using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public static class PresentationFeatureInstaller
{
    private const string MainScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/Presentation VR/Install Training Features")]
    public static void InstallTrainingFeatures()
    {
        if (EditorSceneManager.GetActiveScene().path != MainScenePath)
        {
            EditorSceneManager.OpenScene(MainScenePath);
        }

        PresentationSessionManager manager = Object.FindObjectOfType<PresentationSessionManager>();
        if (manager == null)
        {
            GameObject managerObject = new GameObject("Presentation Session Manager");
            manager = managerObject.AddComponent<PresentationSessionManager>();
        }

        SlideChanger slideChanger = Object.FindObjectOfType<SlideChanger>();
        SpeakerTimer speakerTimer = Object.FindObjectOfType<SpeakerTimer>();
        AudienceReactionController audience = Object.FindObjectOfType<AudienceReactionController>();

        if (speakerTimer == null)
        {
            GameObject timerTextObject = GameObject.Find("TimerText");
            if (timerTextObject != null)
            {
                speakerTimer = timerTextObject.AddComponent<SpeakerTimer>();
                speakerTimer.timerText = timerTextObject.GetComponent<TextMeshPro>();
            }
            else
            {
                GameObject timerObject = new GameObject("Speaker Timer");
                speakerTimer = timerObject.AddComponent<SpeakerTimer>();
            }
        }

        if (audience == null)
        {
            audience = manager.gameObject.AddComponent<AudienceReactionController>();
        }

        manager.slideChanger = slideChanger;
        manager.speakerTimer = speakerTimer;
        manager.audience = audience;
        manager.createMenuOnStart = false;
        manager.autoStartTrainingScene = true;

        GameObject clicker = GameObject.Find("Clicker");
        if (clicker == null)
        {
            XRGrabInteractable grab = Object.FindObjectOfType<XRGrabInteractable>();
            clicker = grab != null ? grab.gameObject : null;
        }

        if (clicker != null)
        {
            ClickerControls clickerControls = clicker.GetComponent<ClickerControls>();
            if (clickerControls == null)
            {
                clickerControls = clicker.AddComponent<ClickerControls>();
            }

            clickerControls.sessionManager = manager;
            clickerControls.createButtonPanel = true;
            clickerControls.useActivateForNextSlide = false;
            clickerControls.dockOnStandWhenReleased = true;
        }
        else
        {
            Debug.LogWarning("Clicker object was not found. Add ClickerControls manually to the clicker object.");
        }

        EditorUtility.SetDirty(manager);
        if (speakerTimer != null) EditorUtility.SetDirty(speakerTimer);
        if (audience != null) EditorUtility.SetDirty(audience);
        if (clicker != null) EditorUtility.SetDirty(clicker);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("Presentation training features installed: separate main menu, statistics, clicker controls and audience reactions.");
    }
}
