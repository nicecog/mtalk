using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SmallPoseGuide : MonoBehaviour {
    public FlowManager fm;
    public RawImage smallPlate;
    public VideoCaptureCam vcc;
    public Text txt;
    public Button NextButton;
    public Button RecordButton;

    Coroutine setupRoutine;
    Coroutine countRoutine;
    bool recordWired;
    bool webcamReady;
    string defaultPrompt;

    void Awake() {
        WireRecordButton();
        if (txt != null)
            defaultPrompt = txt.text;
    }

    void WireRecordButton() {
        if (recordWired || RecordButton == null)
            return;

        RecordButton.onClick.RemoveListener(HandleRecordClicked);
        RecordButton.onClick.AddListener(HandleRecordClicked);
        recordWired = true;
    }

    void HandleRecordClicked() {
        if (!webcamReady) {
            MBodyDiagLog.Warn("SmallPoseGuide", "Record ignored; webcam not ready — retrying setup");
            if (setupRoutine == null)
                setupRoutine = StartCoroutine(SetupCaptureRoutine());
            return;
        }

        MBodyDiagLog.Step("SmallPoseGuide", $"Record clicked page='{name}'");
        startCount();
    }

    void ResolveRefs() {
        if (fm == null)
            fm = FindFirstObjectByType<FlowManager>(FindObjectsInactive.Include);
        if (fm != null && smallPlate == null)
            smallPlate = fm.smallPlate;
        if (vcc == null)
            vcc = FindFirstObjectByType<VideoCaptureCam>(FindObjectsInactive.Include);
        if (txt == null)
            txt = GetComponentInChildren<Text>(true);
    }

    private void OnEnable() {
        MBodyDiagLog.Step("SmallPoseGuide", $"OnEnable page='{name}'");
        webcamReady = false;
        ResolveRefs();
        WireRecordButton();
        SetRecordInteractable(false);
        if (setupRoutine != null)
            StopCoroutine(setupRoutine);
        setupRoutine = StartCoroutine(SetupCaptureRoutine());
    }

    IEnumerator SetupCaptureRoutine() {
        yield return null;
        ResolveRefs();

        if (fm == null || fm.WebCamObject == null) {
            MBodyDiagLog.Warn("SmallPoseGuide", "FlowManager.WebCamObject missing");
            ShowStatus("카메라를 찾을 수 없습니다. 다시 시도해 주세요.");
            SetRecordInteractable(true);
            setupRoutine = null;
            yield break;
        }

        if (txt != null && string.IsNullOrEmpty(defaultPrompt))
            defaultPrompt = txt.text;

        ShowStatus("카메라 준비 중...");
        MBodyDiagLog.Step("SmallPoseGuide", "Waiting for webcam capture...");

        // Parent webcam under this page so DanceTraining pack cannot cover Front.
        yield return BodyResourceLifecycle.EnsureSmallPlateWebcamReady(name == "LV4Cali", transform);

        // Keep guide overlays / buttons above the live camera feed.
        BringGuideUiToFront();

        if (NextButton != null)
            NextButton.gameObject.SetActive(false);
        if (RecordButton != null)
            RecordButton.gameObject.SetActive(true);

        var wci = FindFirstObjectByType<WebCamInput>(FindObjectsInactive.Include);
        webcamReady = wci != null && wci.HasValidFrame
                      && fm.WebCamObject != null
                      && fm.WebCamObject.enabled
                      && fm.WebCamObject.texture != null;

        if (webcamReady) {
            RestorePrompt();
            SetRecordInteractable(true);
            var rt = fm.WebCamObject.rectTransform;
            MBodyDiagLog.Step("SmallPoseGuide",
                $"Setup complete webcam=ON parent={rt.parent.name} pos={rt.anchoredPosition} size={rt.sizeDelta}");
        } else {
            ShowStatus("카메라 연결 실패. 버튼을 다시 눌러 재시도하세요.");
            // Allow tap to retry instead of freezing the page.
            SetRecordInteractable(true);
            MBodyDiagLog.Warn("SmallPoseGuide",
                $"Setup failed readyFrame={(wci != null && wci.HasValidFrame)} camEnabled={(fm.WebCamObject != null && fm.WebCamObject.enabled)}");
        }

        setupRoutine = null;
    }

    void ShowStatus(string message) {
        if (txt == null)
            return;
        txt.text = message;
        var parentImage = txt.transform.parent != null
            ? txt.transform.parent.GetComponent<Image>()
            : null;
        if (parentImage != null)
            parentImage.enabled = true;
    }

    void RestorePrompt() {
        if (txt == null)
            return;
        if (!string.IsNullOrEmpty(defaultPrompt))
            txt.text = defaultPrompt;
        var parentImage = txt.transform.parent != null
            ? txt.transform.parent.GetComponent<Image>()
            : null;
        if (parentImage != null)
            parentImage.enabled = true;
    }

    void SetRecordInteractable(bool interactable) {
        if (RecordButton == null)
            return;

        RecordButton.interactable = interactable;
        var colors = RecordButton.colors;
        colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
        RecordButton.colors = colors;
    }

    void BringGuideUiToFront() {
        // Webcam is first sibling under this page; push text/buttons above it.
        if (RecordButton != null)
            RecordButton.transform.SetAsLastSibling();
        if (NextButton != null)
            NextButton.transform.SetAsLastSibling();
        if (txt != null && txt.transform.parent != null)
            txt.transform.parent.SetAsLastSibling();
    }

    public void startCount() {
        if (countRoutine != null)
            return;

        if (!webcamReady) {
            if (setupRoutine == null)
                setupRoutine = StartCoroutine(SetupCaptureRoutine());
            return;
        }

        ResolveRefs();
        if (RecordButton != null)
            RecordButton.gameObject.SetActive(false);

        countRoutine = StartCoroutine(waitCount());
    }

    private IEnumerator waitCount() {
        for (int i = 3; i > 0; i--) {
            if (txt != null)
                txt.text = String.Concat(i);
            yield return new WaitForSecondsRealtime(1f);
        }

        if (txt != null) {
            txt.text = "";
            var parentImage = txt.transform.parent != null
                ? txt.transform.parent.GetComponent<Image>()
                : null;
            if (parentImage != null)
                parentImage.enabled = false;
        }

        // Auto-advance into the play page so stage 2/3 music+notes start without an extra tap.
        // Logs showed users stopping on LV3Calib after the 3s photo with only "Next" visible.
        var playPageName = name == "LV4Cali" ? "LV4Game" : "LV3Play";
        var playPage = SceneFlowRegistry.Get(playPageName);
        if (playPage != null) {
            MBodyDiagLog.Step("SmallPoseGuide",
                $"Countdown complete; auto-start '{playPageName}' GameLevel play");
            countRoutine = null;
            SceneFlowNavigator.ShowOnly(playPage, gameObject);
            yield break;
        }

        if (NextButton != null)
            NextButton.gameObject.SetActive(true);

        countRoutine = null;
        MBodyDiagLog.Step("SmallPoseGuide", "Countdown complete; NextButton shown (no play page found)");
    }

    private void OnDisable() {
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

        // Keep capture alive for LV3Play, but put webcam back under Front when leaving calib.
        BodyResourceLifecycle.RestoreWebcamHome();
        webcamReady = false;
        recordWired = false;
    }
}
