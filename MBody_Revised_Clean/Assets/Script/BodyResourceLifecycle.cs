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
    static Transform webcamHomeParent;
    static int webcamHomeSibling;

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

        // First attempt + one forced restart if the previous release left capture stuck.
        for (int attempt = 0; attempt < 2; attempt++) {
            if (cachedWebCam != null)
                cachedWebCam.EnsureCapture();

            for (int i = 0; i < 180; i++) {
                if (cachedWebCam != null && cachedWebCam.HasValidFrame)
                    break;
                yield return null;
            }

            if (cachedWebCam != null && cachedWebCam.HasValidFrame)
                break;

            if (attempt == 0 && cachedWebCam != null) {
                MBodyDiagLog.Warn("BodyCapture", "Webcam not ready; restarting capture");
                cachedWebCam.RestartCapture();
            }
        }

        if (cachedWebCam != null && cachedWebCam.HasValidFrame) {
            var readyTex = cachedWebCam.inputImageTexture;
            MBodyDiagLog.Step("BodyCapture", $"Webcam ready texture={readyTex.width}x{readyTex.height}");
            ShowWebcamFeed(readyTex);
        } else {
            MBodyDiagLog.Warn("BodyCapture",
                $"Webcam texture not ready wci={(cachedWebCam != null)} valid={(cachedWebCam != null && cachedWebCam.HasValidFrame)}");
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
            var fm = Object.FindFirstObjectByType<FlowManager>(FindObjectsInactive.Include);
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

        var flow = Object.FindFirstObjectByType<FlowManager>(FindObjectsInactive.Include);
        if (flow != null && flow.WebCamObject != null) {
            flow.WebCamObject.gameObject.SetActive(true);
            flow.WebCamObject.raycastTarget = false;
            // Keep disabled until a real frame exists — avoids full-screen white RawImage.
            flow.WebCamObject.enabled = false;
            flow.WebCamObject.texture = null;
            flow.WebCamObject.color = Color.white;
        }

        if (cachedInputImage != null)
            cachedInputImage.SetActive(true);

        if (cachedPose != null)
            cachedPose.enabled = true;
    }

    static void ShowWebcamFeed(Texture tex)
    {
        if (tex == null)
            return;

        var flow = Object.FindFirstObjectByType<FlowManager>(FindObjectsInactive.Include);
        if (flow != null && flow.WebCamObject != null) {
            flow.WebCamObject.texture = tex;
            flow.WebCamObject.color = Color.white;
            flow.WebCamObject.raycastTarget = false;
            flow.WebCamObject.enabled = true;
            flow.WebCamObject.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Walk up to the SceneFlow page root. NoteManager lives on NoteBox (left panel);
    /// parenting the webcam there shifts SmallPlate coords to the bottom-left.
    /// </summary>
    public static Transform FindSceneFlowHost(Transform start)
    {
        var t = start;
        while (t != null) {
            if (t.CompareTag("SceneFlow"))
                return t;
            t = t.parent;
        }
        return start;
    }

    /// <summary>
    /// Parent the shared webcam under the active SceneFlow page so dance-pack UI cannot cover it.
    /// Coordinates stay in page-center space (same as Front/SmallPlate).
    /// </summary>
    public static void AttachWebcamUnderPage(Transform page)
    {
        var flow = Object.FindFirstObjectByType<FlowManager>(FindObjectsInactive.Include);
        if (flow == null || flow.WebCamObject == null || page == null)
            return;

        var host = FindSceneFlowHost(page);
        var cam = flow.WebCamObject.transform;
        if (webcamHomeParent == null) {
            webcamHomeParent = cam.parent;
            webcamHomeSibling = cam.GetSiblingIndex();
        }

        if (cam.parent != host) {
            cam.SetParent(host, false);
            cam.SetAsFirstSibling();
            MBodyDiagLog.Step("BodyCapture",
                $"Webcam attached under page '{host.name}' (from '{page.name}')");
        } else {
            cam.SetAsFirstSibling();
        }
    }

    public static void RestoreWebcamHome()
    {
        var flow = Object.FindFirstObjectByType<FlowManager>(FindObjectsInactive.Include);
        if (flow == null || flow.WebCamObject == null || webcamHomeParent == null)
            return;

        var cam = flow.WebCamObject.transform;
        if (cam.parent == webcamHomeParent)
            return;

        cam.SetParent(webcamHomeParent, false);
        var max = webcamHomeParent.childCount - 1;
        cam.SetSiblingIndex(Mathf.Clamp(webcamHomeSibling, 0, max));
        MBodyDiagLog.Step("BodyCapture", "Webcam restored to Front");
    }

    /// <summary>
    /// Positions the shared webcam RawImage for LV3/LV4 dance calibration and play pages.
    /// </summary>
    public static void ConfigureSmallPlateWebcam(bool lv4Layout = false)
    {
        var flow = Object.FindFirstObjectByType<FlowManager>(FindObjectsInactive.Include);
        if (flow == null || flow.WebCamObject == null)
            return;

        // SmallPlate marker under Front: (337, -182), size (1952, 1252) — right-side self view.
        var plate = flow.smallPlate;
        Vector2 size;
        Vector2 pos;
        if (plate != null) {
            size = plate.rectTransform.sizeDelta;
            pos = plate.rectTransform.anchoredPosition;
        } else {
            size = new Vector2(1952f, 1252f);
            pos = new Vector2(337f, -182f);
        }

        var cam = flow.WebCamObject;
        var rt = cam.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        flow.SmallScale(size, pos);

        cam.gameObject.SetActive(true);
        cam.raycastTarget = false;
        cam.uvRect = lv4Layout
            ? new Rect(0.8f, 0f, -0.6f, 1f)
            : new Rect(1f, 0f, -1f, 1f);

        // Only show when we have a live texture — never flash a white empty RawImage.
        var hasTex = cam.texture != null;
        cam.enabled = hasTex;
        if (!hasTex)
            cam.color = Color.white;

        foreach (Transform child in cam.transform) {
            if (child.GetComponent<Text>() != null)
                child.gameObject.SetActive(false);
        }

        MBodyDiagLog.Step("BodyCapture",
            $"SmallPlate layout parent='{(cam.transform.parent != null ? cam.transform.parent.name : "null")}' pos={pos} size={size}");
    }

    public static IEnumerator EnsureSmallPlateWebcamReady(bool lv4Layout = false, Transform hostPage = null)
    {
        if (hostPage != null)
            AttachWebcamUnderPage(hostPage);

        yield return EnsureBodyCaptureWhenReady();
        ConfigureSmallPlateWebcam(lv4Layout);

        // Re-apply texture after layout in case Configure cleared visibility.
        if (cachedWebCam != null && cachedWebCam.HasValidFrame)
            ShowWebcamFeed(cachedWebCam.inputImageTexture);
    }

    static void HideWebcamFeed()
    {
        var flow = Object.FindFirstObjectByType<FlowManager>(FindObjectsInactive.Include);
        if (flow != null && flow.WebCamObject != null) {
            flow.WebCamObject.enabled = false;
            flow.WebCamObject.texture = null;
        }
    }

    /// <summary>
    /// Called before SceneFlow page swaps so a webcam parented under LV3/LV4 play pages
    /// does not stay visible on selection screens (ghost overlap behind UI).
    /// </summary>
    public static void PrepareForPageTransition()
    {
        var flow = Object.FindFirstObjectByType<FlowManager>(FindObjectsInactive.Include);
        if (flow == null || flow.WebCamObject == null)
            return;

        var cam = flow.WebCamObject.transform;
        var host = FindSceneFlowHost(cam);
        if (host != null && host != cam && cam.IsChildOf(host))
            RestoreWebcamHome();

        HideWebcamFeed();
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
        RestoreWebcamHome();

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

        var fm = Object.FindFirstObjectByType<FlowManager>(FindObjectsInactive.Include);
        if (fm != null && fm.WebCamObject != null)
            fm.WebCamObject.gameObject.SetActive(false);

        cachedWebCam = null;
        cachedPose = null;
        cachedInputImage = null;
    }
}
