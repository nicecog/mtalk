using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using NatSuite.Recorders;
using NatSuite.Recorders.Clocks;
using NatSuite.Recorders.Inputs;

public class VideoCaptureCam : MonoBehaviour {
    public DanceManager dm;
    public WebCamInput wci;
    public RawImage img;
    public JsonRequest jr;
    public AudioListener al;
    public string prefix;
    public string latestMovie;

    MP4Recorder recorder;
    IClock clock;
    ITextureInput textureInput;
    AudioInput audioInput;
    bool recording;
    bool stopping;
    int commitCounter;
    Coroutine uploadRoutine;

    // Tab A8 is memory-tight: commit every Nth webcam frame while recording.
    const int CommitEveryNthFrame = 2;

    public bool IsRecording => recording;

    void Awake() {
        jr = FindFirstObjectByType<JsonRequest>();
    }

    void OnDisable() {
        if (recording)
            StopRecordingSafe(false, 0, upload: false);
    }

    void OnDestroy() {
        if (recording)
            StopRecordingSafe(false, 0, upload: false);
    }

    public void startRecord() {
        ResolveRefs();
        if (wci == null || wci.webCamTexture == null) {
            MBodyDiagLog.Warn("VideoCapture",
                $"startRecord blocked wci={(wci != null)} webCamTexture={(wci != null && wci.webCamTexture != null)}");
            return;
        }

        // Never stack recorders — previous stage-3 runs left native buffers and OOM'd Tab A8.
        if (recording || recorder != null || stopping) {
            MBodyDiagLog.Warn("VideoCapture", "startRecord while busy; aborting previous recorder");
            StopRecordingSafe(false, 0, upload: false);
        }

        try {
            var performance = PerformanceManager.Instance;
            var recordWidth = performance != null ? performance.RecordWidth : 640;
            var recordHeight = performance != null ? performance.RecordHeight : 480;
            var recordFps = performance != null ? performance.RecordFps : 15f;
            // Cap fps for long LV3 stage-3 takes (~2 min).
            recordFps = Mathf.Min(recordFps, 15f);

            MBodyDiagLog.Step("VideoCapture", $"startRecord {recordWidth}x{recordHeight}@{recordFps}fps");

            clock = new RealtimeClock();
            recorder = new MP4Recorder(
                recordWidth,
                recordHeight,
                recordFps,
                (int)AudioSettings.outputSampleRate,
                (int)AudioSettings.speakerMode);

            textureInput = SystemInfo.supportsAsyncGPUReadback
                ? new AsyncTextureInput(recorder)
                : new TextureInput(recorder);

            if (al == null)
                al = FindFirstObjectByType<AudioListener>(FindObjectsInactive.Include);

            if (al != null)
                audioInput = new AudioInput(recorder, clock, al);
            else
                MBodyDiagLog.Warn("VideoCapture", "AudioListener missing; recording video only");

            commitCounter = 0;
            performance?.SetRecordingActive(true);
            recording = true;
            MBodyDiagLog.Step("VideoCapture", "startRecord active");
        } catch (Exception ex) {
            MBodyDiagLog.Exception("VideoCapture", "startRecord", ex);
            DisposeInputs();
            recorder = null;
            recording = false;
            PerformanceManager.Instance?.SetRecordingActive(false);
        }
    }

    void ResolveRefs() {
        if (wci == null)
            wci = FindFirstObjectByType<WebCamInput>(FindObjectsInactive.Include);
        if (dm == null)
            dm = FindFirstObjectByType<DanceManager>(FindObjectsInactive.Include);
        if (jr == null)
            jr = FindFirstObjectByType<JsonRequest>();
        if (al == null)
            al = FindFirstObjectByType<AudioListener>(FindObjectsInactive.Include);
    }

    void Update() {
        if (!recording || wci == null || textureInput == null || clock == null)
            return;
        if (wci.webCamTexture == null || !wci.webCamTexture.didUpdateThisFrame)
            return;

        commitCounter++;
        if (commitCounter % CommitEveryNthFrame != 0)
            return;

        var tex = wci.inputImageTexture;
        if (tex == null)
            return;

        try {
            textureInput.CommitFrame(tex, clock.timestamp);
        } catch (Exception ex) {
            MBodyDiagLog.Exception("VideoCapture", "CommitFrame", ex);
            StopRecordingSafe(false, 0, upload: false);
        }
    }

    public void stopRecording(bool gamePlay = false, int prePost = 0) {
        StopRecordingSafe(gamePlay, prePost, upload: gamePlay || prePost > 0);
    }

    void StopRecordingSafe(bool gamePlay, int prePost, bool upload) {
        if (stopping)
            return;

        MBodyDiagLog.Step("VideoCapture",
            $"stopRecording gamePlay={gamePlay} prePost={prePost} upload={upload} recording={recording}");

        recording = false;
        stopping = true;
        PerformanceManager.Instance?.SetRecordingActive(false);
        DisposeInputs();

        var activeRecorder = recorder;
        recorder = null;
        if (activeRecorder == null) {
            stopping = false;
            MBodyDiagLog.Warn("VideoCapture", "stopRecording skipped; recorder null");
            return;
        }

        StartCoroutine(FinishRecordingCoroutine(activeRecorder, gamePlay, upload));
    }

    void DisposeInputs() {
        try { audioInput?.Dispose(); } catch (Exception ex) {
            MBodyDiagLog.Exception("VideoCapture", "audioInput.Dispose", ex);
        }
        try { textureInput?.Dispose(); } catch (Exception ex) {
            MBodyDiagLog.Exception("VideoCapture", "textureInput.Dispose", ex);
        }
        audioInput = null;
        textureInput = null;
    }

    IEnumerator FinishRecordingCoroutine(MP4Recorder activeRecorder, bool gamePlay, bool upload) {
        string path = null;
        var finish = activeRecorder.FinishWriting();
        while (!finish.IsCompleted)
            yield return null;

        stopping = false;

        try {
            if (finish.IsFaulted) {
                MBodyDiagLog.Exception("VideoCapture", "FinishWriting", finish.Exception);
                yield break;
            }
            path = finish.Result;
        } catch (Exception ex) {
            MBodyDiagLog.Exception("VideoCapture", "FinishWriting result", ex);
            yield break;
        }

        MBodyDiagLog.Step("VideoCapture", $"Saved recording temp path={path}");
        ResolveRefs();
        if (dm == null || string.IsNullOrEmpty(path) || !File.Exists(path)) {
            MBodyDiagLog.Warn("VideoCapture", "Cannot finalize movie path");
            yield break;
        }

        string finalPath;
        try {
            finalPath = fileSystem();
            if (File.Exists(finalPath))
                File.Delete(finalPath);
            File.Move(path, finalPath);
        } catch (Exception ex) {
            MBodyDiagLog.Exception("VideoCapture", "File.Move", ex);
            yield break;
        }

        if (gamePlay)
            latestMovie = finalPath;

        MBodyDiagLog.Step("VideoCapture", $"Recording finalized path={finalPath}");

        // Defer upload: File.ReadAllBytes of a ~2min clip during LV3Play caused Tab A8 OOM.
        if (upload) {
            if (uploadRoutine != null)
                StopCoroutine(uploadRoutine);
            uploadRoutine = StartCoroutine(DeferredUpload(finalPath));
        }

        // Encourage native/managed cleanup after a long encode.
        yield return null;
        Resources.UnloadUnusedAssets();
        GC.Collect();
    }

    IEnumerator DeferredUpload(string path) {
        // Wait until play UI has settled and previous GPU work is idle.
        yield return new WaitForSecondsRealtime(2f);
        Resources.UnloadUnusedAssets();
        GC.Collect();
        yield return null;

        if (jr == null)
            jr = FindFirstObjectByType<JsonRequest>();
        if (jr == null || !File.Exists(path)) {
            uploadRoutine = null;
            yield break;
        }

        MBodyDiagLog.Step("VideoCapture", $"Deferred upload begin path={path}");
        jr.uploadMovie(path);
        uploadRoutine = null;
    }

    public void setPrefix(string pr) {
        prefix = pr;
    }

    public string fileSystem() {
        string id = dm != null && !string.IsNullOrEmpty(dm.ID) ? dm.ID : "player";
        string filename = id + "_" + prefix + "_" + DateTime.Now.ToString("yy-MM-dd HH-mm-ss") + ".mp4";
        return Path.Combine(MBodyPaths.DataRoot, filename);
    }
}
