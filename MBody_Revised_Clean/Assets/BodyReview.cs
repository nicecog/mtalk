using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class BodyReview : MonoBehaviour
{
    void Awake() {
        dm = FindFirstObjectByType<DanceManager>();
        vp = gameObject.AddComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.targetTexture = td;
        vp.audioOutputMode = VideoAudioOutputMode.None;
        vp.waitForFirstFrame = true;
    }

    public VideoPlayer vp;
    public DanceManager dm;
    private List<string> thirds;

    private void OnEnable() {
        StartCoroutine(prefare());
    }

    static bool IsBodyRecording(string path)
    {
        return path.Contains("BodyThird")
            || path.Contains("BodyAlign")
            || path.Contains("BodyThirdPlay");
    }

    public IEnumerator prefare() {
        if (dm == null)
            dm = FindFirstObjectByType<DanceManager>();
        if (dm == null || string.IsNullOrEmpty(dm.ID))
        {
            MBodyDiagLog.Warn("BodyReview", "DanceManager/ID missing; cannot list recordings");
            yield break;
        }

        thirds = new List<string>();
        string[] t = Directory.GetFiles(MBodyPaths.DataRoot);
        for (int i = t.Length - 1; i >= 0; i--) {
            if (t[i].Contains(dm.ID) && !t[i].Contains(".meta") && IsBodyRecording(t[i]))
                thirds.Add(t[i]);
        }

        MBodyDiagLog.Step("BodyReview", $"Found {thirds.Count} body recordings for id={dm.ID}");

        for (int i = 0; i < dsSprites.Length && i < thirds.Count; i++) {
            dsSprites[i].interactable = false;
            MBodyDiagLog.Step("BodyReview", $"Thumbnail {i}: {Path.GetFileName(thirds[i])}");
            yield return VideoThumbnailGenerator.CaptureButtonThumbnail(vp, td, mat, dsSprites[i], thirds[i]);
        }
    }

    public void clickButton(int i) {
        if (thirds == null || i < 0 || i >= thirds.Count)
        {
            MBodyDiagLog.Warn("BodyReview", $"clickButton invalid index={i} count={thirds?.Count ?? 0}");
            return;
        }

        var path = thirds[i];
        MBodyDiagLog.Step("BodyReview", $"Play recording index={i} path={path}");
        outPlayer.url = path;

        if (outPlayer.GetComponent<UiVideoPlayer>() == null)
            outPlayer.gameObject.AddComponent<UiVideoPlayer>();

        MoviePop.SetActive(true);

        if (ScenePageVideoDriver.Instance != null)
            ScenePageVideoDriver.Instance.PlayPlayer(outPlayer);
    }

    public Button[] dsSprites;
    public GameObject MoviePop;
    public VideoPlayer outPlayer;
    public Shader mat;
    public RenderTexture td;
}
