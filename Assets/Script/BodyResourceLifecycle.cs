using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Releases Body-mode capture (webcam + pose) when entering Dance-only pages.
/// </summary>
public static class BodyResourceLifecycle
{
    static WebCamInput cachedWebCam;
    static PoseVisuallizer cachedPose;
    static GameObject cachedInputImage;

    public static void EnsureBodyCapture()
    {
        ResolveCaptureRefs();
        ActivateWebcamUi();
        MBodyDiagLog.Step("BodyCapture", "EnsureBodyCapture webcam enabled");
    }

    public static IEnumerator EnsureBodyCaptureWhenReady()
    {
        ResolveCaptureRefs();
        ActivateWebcamUi();

        for (int i = 0; i < 180; i++) {
            if (cachedWebCam != null) {
                var tex = cachedWebCam.inputImageTexture;
                if (tex != null && tex.width > 16 && tex.height > 16)
                    break;
            }
            yield return null;
        }

        var readyTex = cachedWebCam != null ? cachedWebCam.inputImageTexture : null;
        if (readyTex != null && readyTex.width > 16 && readyTex.height > 16) {
            if (cachedPose != null)
                cachedPose.enabled = true;
            MBodyDiagLog.Step("BodyCapture", $"Pose enabled texture={readyTex.width}x{readyTex.height}");
        } else {
            MBodyDiagLog.Warn("BodyCapture", "Webcam texture not ready; pose tracking left disabled");
        }
    }

    static void ResolveCaptureRefs()
    {
        if (cachedWebCam == null)
            cachedWebCam = Object.FindFirstObjectByType<WebCamInput>(FindObjectsInactive.Include);
        if (cachedPose == null)
            cachedPose = Object.FindFirstObjectByType<PoseVisuallizer>(FindObjectsInactive.Include);

        if (cachedInputImage == null) {
            var fm = Object.FindFirstObjectByType<FlowManager>();
            if (fm != null && fm.WebCamObject != null)
                cachedInputImage = fm.WebCamObject.gameObject;
            else if (cachedWebCam != null)
                cachedInputImage = cachedWebCam.gameObject;
        }
    }

    static void ActivateWebcamUi()
    {
        if (cachedInputImage != null)
            cachedInputImage.SetActive(true);

        if (cachedWebCam != null) {
            cachedWebCam.gameObject.SetActive(true);
            cachedWebCam.EnsureCapture();
        }

        var flow = Object.FindFirstObjectByType<FlowManager>();
        if (flow != null && flow.WebCamObject != null) {
            flow.WebCamObject.gameObject.SetActive(true);
            flow.WebCamObject.enabled = true;
            flow.WebCamObject.raycastTarget = false;
            flow.OriginalScale();
            flow.WebCamObject.uvRect = new Rect(1f, 0, -1f, 1f);
        }
    }
    /// <summary>
    /// Gates pose compute dispatch so it only runs on actual game pages.
    /// Keeps the webcam + detecter alive to avoid buffer churn on transitions.
    /// </summary>
    public static void SetPoseProcessing(bool active)
    {
        if (cachedPose == null)
            cachedPose = Object.FindFirstObjectByType<PoseVisuallizer>(FindObjectsInactive.Include);

        if (cachedPose != null)
            cachedPose.SetProcessing(active);
    }

    public static void ReleaseBodyCapture()
    {
        if (cachedWebCam == null)
            cachedWebCam = Object.FindFirstObjectByType<WebCamInput>(FindObjectsInactive.Include);
        if (cachedPose == null)
            cachedPose = Object.FindFirstObjectByType<PoseVisuallizer>(FindObjectsInactive.Include);

        if (cachedInputImage == null && cachedWebCam != null)
            cachedInputImage = cachedWebCam.gameObject;

        if (cachedInputImage != null)
            cachedInputImage.SetActive(false);

        if (cachedPose != null)
            cachedPose.enabled = false;

        if (cachedWebCam != null)
            cachedWebCam.ReleaseCapture();

        var fm = Object.FindFirstObjectByType<FlowManager>();
        if (fm != null && fm.WebCamObject != null)
            fm.WebCamObject.gameObject.SetActive(false);

        cachedWebCam = null;
        cachedPose = null;
        cachedInputImage = null;
    }
}
