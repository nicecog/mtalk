using UnityEngine;

/// <summary>
/// Attach to Dance SceneFlow pages to release Body webcam/pose resources on entry.
/// </summary>
public sealed class DanceModePage : MonoBehaviour
{
    void OnEnable()
    {
        BodyResourceLifecycle.ReleaseBodyCapture();
    }
}
