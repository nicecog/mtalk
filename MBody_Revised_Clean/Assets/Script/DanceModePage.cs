using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to Dance SceneFlow pages to release Body webcam/pose resources on entry.
/// PoseGuide pages keep the camera active for alignment recording.
/// </summary>
public sealed class DanceModePage : MonoBehaviour
{
    static readonly HashSet<string> CameraPages = new HashSet<string>
    {
        "PoseGuide",
        "DanceIntroPoseGuide",
        "PoseGuidePost",
        "LV3Calib",
        "LV3Play",
        "LV4Cali",
        "LV4Game",
    };

    void OnEnable()
    {
        if (!CameraPages.Contains(gameObject.name))
            BodyResourceLifecycle.ReleaseBodyCapture();

        // Dance action / level select: ensure a home button to the post-login hub.
        if (gameObject.name == "DanceSelect" || gameObject.name == "DanceLevelSelect") {
            if (GetComponent<DanceHomeButton>() == null)
                gameObject.AddComponent<DanceHomeButton>();
        }
    }
}
