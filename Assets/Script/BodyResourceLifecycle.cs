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
        PrepareWebcamUi();
        MBodyDiagLog.Step("BodyCapture", "EnsureBodyCapture webcam enabled");
    }

    public static IEnumerator EnsureBodyCaptureWhenReady()
    {
        ResolveCaptureRefs();
        PrepareWebcamUi();

        for (int i = 0; i < 180; i++) {
            if (cachedWebCam != null && cachedWebCam.HasValidFrame)
                break;
            yield return null;
        }

        if (cachedWebCam != null && cachedWebCam.HasValidFrame) {
            var readyTex = cachedWebCam.inputImageTexture;
            MBodyDiagLog.Step("BodyCapture", $"Webcam ready texture={readyTex.width}x{readyTex.height}");
            ShowWebcamFeed(readyTex);
        } else {
            MBodyDiagLog.Warn("BodyCapture", "Webcam texture not ready");
            HideWebcamFeed();
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

    static void PrepareWebcamUi()
    {
        if (cachedWebCam != null) {
            cachedWebCam.gameObject.SetActive(true);
            cachedWebCam.EnsureCapture();
        }

        var flow = Object.FindFirstObjectByType<FlowManager>();
        if (flow != null && flow.WebCamObject != null) {
            flow.WebCamObject.gameObject.SetActive(true);
            flow.WebCamObject.raycastTarget = false;
            flow.OriginalScale();
            flow.WebCamObject.uvRect = new Rect(1f, 0, -1f, 1f);
            flow.WebCamObject.enabled = false;
            flow.WebCamObject.texture = null;
        }

        if (cachedInputImage != null)
            cachedInputImage.SetActive(true);

        if (cachedPose != null)
            cachedPose.enabled = true;
    }

    static void ShowWebcamFeed(Texture tex)
    {
        var flow = Object.FindFirstObjectByType<FlowManager>();
        if (flow != null && flow.WebCamObject != null) {
            flow.WebCamObject.texture = tex;
            flow.WebCamObject.color = Color.white;
            flow.WebCamObject.enabled = true;
        }
    }

    static void HideWebcamFeed()
    {
        var flow = Object.FindFirstObjectByType<FlowManager>();
        if (flow != null && flow.WebCamObject != null)
            flow.WebCamObject.enabled = false;
    }

    public static void SetPoseProcessing(bool active)
    {
        if (cachedPose == null)
            cachedPose = Object.FindFirstObjectByType<PoseVisuallizer>(FindObjectsInactive.Include);

        if (cachedPose != null)
            cachedPose.SetProcessing(active);
    }

    public static void ReleaseBodyCapture()
    {
        SetPoseProcessing(false);

        if (cachedWebCam == null)
            cachedWebCam = Object.FindFirstObjectByType<WebCamInput>(FindObjectsInactive.Include);
        if (cachedPose == null)
            cachedPose = Object.FindFirstObjectByType<PoseVisuallizer>(FindObjectsInactive.Include);

        if (cachedInputImage == null && cachedWebCam != null)
            cachedInputImage = cachedWebCam.gameObject;

        HideWebcamFeed();

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
