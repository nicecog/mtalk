using System;
using UnityEngine;
using UnityEngine.UI;
using NatSuite.Recorders;
using NatSuite.Recorders.Clocks;
using NatSuite.Recorders.Inputs;
using System.IO;

public class VideoCaptureCam : MonoBehaviour {
    public DanceManager dm;
    public WebCamInput wci;

    MP4Recorder recorder;
    IClock clock;
    ITextureInput textureInput;
    bool recording;

    void Awake() {
        jr = FindFirstObjectByType<JsonRequest>();
    }

    public RawImage img;
    AudioInput audioInput;

    public void startRecord() {
        if (wci == null || wci.webCamTexture == null) {
            Debug.LogWarning("VideoCaptureCam: WebCam is not ready.");
            return;
        }

        var performance = PerformanceManager.Instance;
        var recordWidth = performance != null ? performance.RecordWidth : 1280;
        var recordHeight = performance != null ? performance.RecordHeight : 720;
        var recordFps = performance != null ? performance.RecordFps : 25f;

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

        audioInput = new AudioInput(recorder, clock, al);
        performance?.SetRecordingActive(true);
        recording = true;
    }

    void Update()
    {
        if (!recording || wci == null || wci.webCamTexture == null || textureInput == null)
            return;

        if (!wci.webCamTexture.didUpdateThisFrame)
            return;

        textureInput.CommitFrame(wci.inputImageTexture, clock.timestamp);
    }

    public async void stopRecording(bool gamePlay=false, int prePost=0) {
        Debug.Log("Stop Recording");
        recording = false;
        PerformanceManager.Instance?.SetRecordingActive(false);

        audioInput?.Dispose();
        textureInput?.Dispose();
        textureInput = null;

        if (recorder == null)
            return;

        var path = await recorder.FinishWriting();
        recorder = null;

        Debug.Log($"Saved recording to: {path}");
        string s = fileSystem();
        File.Move(path,s);
        if (gamePlay)
            latestMovie = s;
        if(gamePlay||prePost>0)
            jr.uploadMovie(s);
    }

    public JsonRequest jr;

    public void setPrefix(string pr) {
        prefix = pr;
    }
    public string prefix;

    public string fileSystem() {
        string filename = dm.ID + "_" + prefix + "_" + DateTime.Now.ToString("yy-MM-dd HH-mm-ss") + ".mp4";
        return Path.Combine(MBodyPaths.DataRoot, filename);
    }

    public string latestMovie;

    public AudioListener al;
}
