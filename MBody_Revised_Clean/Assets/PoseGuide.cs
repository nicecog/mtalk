using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PoseGuide : MonoBehaviour {
    public RawImage HalfCam;

    public VideoCaptureCam vcc;
    public Text txt;
    public Button NextButton;
    public Button RecordButton;

    Coroutine setupRoutine;
    Coroutine countRoutine;
    bool recordWired;
    bool postExitWired;

    void Awake() {
        WireRecordButton();
    }

    void WireRecordButton() {
        if (recordWired || RecordButton == null)
            return;

        RecordButton.onClick.AddListener(HandleRecordClicked);
        recordWired = true;
        MBodyDiagLog.Step("PoseGuide", $"RecordButton wired on '{name}' id={gameObject.GetInstanceID()}");
    }

    void HandleRecordClicked() {
        MBodyDiagLog.Step("PoseGuide", $"RecordButton clicked page='{name}' id={gameObject.GetInstanceID()} active={gameObject.activeInHierarchy}");

        var proxy = FindFirstObjectByType<PackServiceProxy>(FindObjectsInactive.Include);
        if (proxy != null) {
            proxy.setPrefix("Body_Align_Pre");
            MBodyDiagLog.Step("PoseGuide", "setPrefix Body_Align_Pre via PackServiceProxy");
        } else if (vcc != null) {
            vcc.setPrefix("Body_Align_Pre");
            MBodyDiagLog.Step("PoseGuide", "setPrefix Body_Align_Pre via VideoCaptureCam");
        } else {
            MBodyDiagLog.Warn("PoseGuide", "setPrefix skipped; PackServiceProxy and VideoCaptureCam missing");
        }

        startCount();
    }

    void ResolveRefs() {
        if (HalfCam == null) {
            var fm = FindFirstObjectByType<FlowManager>(FindObjectsInactive.Include);
            if (fm != null)
                HalfCam = fm.WebCamObject;
        }

        if (vcc == null)
            vcc = FindFirstObjectByType<VideoCaptureCam>(FindObjectsInactive.Include);
    }

    private void OnEnable() {
        MBodyDiagLog.Step("PoseGuide", $"OnEnable page='{name}' id={gameObject.GetInstanceID()}");
        ResolveRefs();
        WireRecordButton();
        if (setupRoutine != null)
            StopCoroutine(setupRoutine);
        setupRoutine = StartCoroutine(SetupCaptureRoutine());
    }

    IEnumerator SetupCaptureRoutine() {
        yield return null;

        ResolveRefs();
        if (HalfCam == null) {
            MBodyDiagLog.Warn("PoseGuide", "HalfCam not found; webcam preview unavailable");
            yield break;
        }

        MBodyDiagLog.Step("PoseGuide", "Waiting for webcam capture...");
        BodyResourceLifecycle.AttachWebcamUnderPage(transform);
        yield return BodyResourceLifecycle.EnsureBodyCaptureWhenReady();

        ResolveRefs();
        ConfigureWebcamPreview();
        // Only enable feed when a real frame exists (avoid white full-screen RawImage).
        var wciReady = FindFirstObjectByType<WebCamInput>(FindObjectsInactive.Include);
        if (HalfCam != null) {
            HalfCam.raycastTarget = false;
            if (wciReady == null || !wciReady.HasValidFrame) {
                HalfCam.enabled = false;
                HalfCam.texture = null;
            }
        }
        BringGuideUiToFront();

        if (NextButton != null)
            NextButton.gameObject.SetActive(false);
        if (txt != null) {
            var parentImage = txt.transform.parent.GetComponent<Image>();
            if (parentImage != null)
                parentImage.enabled = true;
        }
        if (RecordButton != null)
            RecordButton.gameObject.SetActive(true);

        var wci = FindFirstObjectByType<WebCamInput>(FindObjectsInactive.Include);
        var ready = wci != null && wci.HasValidFrame;
        MBodyDiagLog.Step("PoseGuide", $"Setup complete recordBtn={(RecordButton != null)} txt={(txt != null)} vcc={(vcc != null)} webcamReady={ready}");
        setupRoutine = null;
    }

    void ConfigureWebcamPreview() {
        HalfCam.gameObject.SetActive(true);
        HalfCam.enabled = true;
        HalfCam.raycastTarget = false;
        HalfCam.uvRect = new Rect(1f, 0, -1f, 1f);
        HalfCam.rectTransform.sizeDelta = new Vector2(2800, 1575);
        HalfCam.rectTransform.anchoredPosition = new Vector2(0, -88.5f);
        HideWebcamPlaceholderText();
    }

    static void HideWebcamPlaceholderText() {
        var flow = FindFirstObjectByType<FlowManager>(FindObjectsInactive.Include);
        if (flow == null || flow.WebCamObject == null)
            return;

        foreach (Transform child in flow.WebCamObject.transform) {
            if (child.GetComponent<Text>() != null)
                child.gameObject.SetActive(false);
        }
    }

    void BringGuideUiToFront() {
        // Order under the Scenes canvas so the webcam preview sits directly behind the
        // PoseGuide page (in front of the opaque background) and the guide overlay/buttons
        // render on top of the live camera feed.
        var flow = FindFirstObjectByType<FlowManager>(FindObjectsInactive.Include);
        Transform front = flow != null ? flow.transform.Find("Front") : null;

        if (front != null && front.parent == transform.parent) {
            front.SetAsLastSibling();
            transform.SetAsLastSibling();
            MBodyDiagLog.Step("PoseGuide", $"Layered webcam behind page front={front.GetSiblingIndex()} page={transform.GetSiblingIndex()}");
        } else {
            transform.SetAsLastSibling();
            MBodyDiagLog.Warn("PoseGuide", $"Front layer not a sibling; cannot order webcam (front={(front != null)})");
        }
    }

    public void startCount() {
        MBodyDiagLog.Step("PoseGuide", $"startCount entered page='{name}' id={gameObject.GetInstanceID()}");

        if (countRoutine != null) {
            MBodyDiagLog.Warn("PoseGuide", "startCount ignored; countdown already running");
            return;
        }

        ResolveRefs();
        if (vcc == null) {
            MBodyDiagLog.Warn("PoseGuide", "VideoCaptureCam not found; cannot start recording");
            return;
        }

        var wci = FindFirstObjectByType<WebCamInput>(FindObjectsInactive.Include);
        if (wci == null) {
            MBodyDiagLog.Warn("PoseGuide", "WebCamInput not found; cannot start recording");
            return;
        }

        if (!wci.HasValidFrame) {
            MBodyDiagLog.Warn("PoseGuide", $"Webcam not ready; texture={(wci.inputImageTexture != null)} playing={(wci.webCamTexture != null && wci.webCamTexture.isPlaying)}");
            return;
        }

        if (vcc.wci == null)
            vcc.wci = wci;

        if (RecordButton != null)
            RecordButton.gameObject.SetActive(false);

        countRoutine = StartCoroutine(waitCount());
    }

    private IEnumerator waitCount() {
        MBodyDiagLog.Step("PoseGuide", "Countdown started (15s)");

        try {
            vcc.startRecord();
            MBodyDiagLog.Step("PoseGuide", "VideoCaptureCam.startRecord OK");
        } catch (Exception ex) {
            MBodyDiagLog.Exception("PoseGuide", "VideoCaptureCam.startRecord", ex);
        }

        for (int i = 15; i > 0; i--) {
            if (txt != null)
                txt.text = String.Concat(i);
            MBodyDiagLog.Step("PoseGuide", $"Countdown tick {i}");
            yield return new WaitForSecondsRealtime(1f);
        }

        if (txt != null) {
            txt.text = "";
            var parentImage = txt.transform.parent.GetComponent<Image>();
            if (parentImage != null)
                parentImage.enabled = false;
        }

        MBodyDiagLog.Step("PoseGuide", "Countdown finished; stopping recording");
        if (vcc != null) {
            try {
                vcc.stopRecording(false, 1);
            } catch (Exception ex) {
                MBodyDiagLog.Exception("PoseGuide", "VideoCaptureCam.stopRecording", ex);
            }
        }

        yield return new WaitForSecondsRealtime(0.5f);

        if (NextButton != null) {
            NextButton.gameObject.SetActive(true);
            WirePostExitButton();
        }

        MBodyDiagLog.Step("PoseGuide", "Countdown flow complete; NextButton shown");
        countRoutine = null;
    }

    void WirePostExitButton() {
        if (postExitWired || NextButton == null || gameObject.name != "PoseGuidePost")
            return;

        NextButton.onClick.AddListener(CompletePoseGuidePost);
        postExitWired = true;
        MBodyDiagLog.Step("PoseGuide", "PoseGuidePost exit wired -> DanceSelect");
    }

    void CompletePoseGuidePost() {
        MBodyDiagLog.Step("PoseGuide", "PoseGuidePost complete; navigating to DanceSelect");
        BodyResourceLifecycle.ReleaseBodyCapture();

        var danceSelect = SceneFlowRegistry.Get("DanceSelect");
        if (danceSelect != null)
            SceneFlowNavigator.ShowOnly(danceSelect, gameObject);
        else
            gameObject.SetActive(false);
    }

    private void OnDisable() {
        MBodyDiagLog.Step("PoseGuide", $"OnDisable page='{name}' id={gameObject.GetInstanceID()}");

        if (setupRoutine != null) {
            StopCoroutine(setupRoutine);
            setupRoutine = null;
        }

        if (countRoutine != null) {
            StopCoroutine(countRoutine);
            countRoutine = null;
        }

        if (RecordButton != null)
            RecordButton.onClick.RemoveListener(HandleRecordClicked);

        if (NextButton != null)
            NextButton.onClick.RemoveListener(CompletePoseGuidePost);

        postExitWired = false;

        BodyResourceLifecycle.RestoreWebcamHome();

        if (HalfCam != null)
            HalfCam.gameObject.SetActive(false);

        if (gameObject.name != "PoseGuidePost")
            BodyResourceLifecycle.ReleaseBodyCapture();
    }
}
