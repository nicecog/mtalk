using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoReview : MonoBehaviour {
    public Button[] dsSprites;
    public Button[] dkSprites;

    void Awake() {
        dm = FindFirstObjectByType<DanceManager>();
        vp = gameObject.AddComponent<VideoPlayer>();
        vp.targetTexture = td;
        vp.audioOutputMode = VideoAudioOutputMode.None;
        vp.playOnAwake = false;
        vp.waitForFirstFrame = true;
    }

    public List<string> DKfiles;
    public List<string> DSfiles;

    private void OnEnable() {
        StartCoroutine(prefare());
    }

    IEnumerator prefare() {
        DSfiles = new List<string>();
        DKfiles = new List<string>();
        string[] t = Directory.GetFiles(MBodyPaths.DataRoot);
        for (int i = t.Length - 1; i >= 0; i--) {
            if (!t[i].Contains(dm.ID) || t[i].Contains(".meta"))
                continue;
            if (t[i].Contains("DanceKing"))
                DKfiles.Add(t[i]);
            else if (t[i].Contains("DanceThird"))
                DSfiles.Add(t[i]);
        }

        for (int i = 0; i < 4 && i < DSfiles.Count; i++) {
            Debug.Log(DSfiles[i]);
            yield return VideoThumbnailGenerator.CaptureButtonThumbnail(vp, td, mat, dsSprites[i], DSfiles[i]);
        }

        for (int i = 0; i < 4 && i < DKfiles.Count; i++) {
            Debug.Log(DKfiles[i]);
            yield return VideoThumbnailGenerator.CaptureButtonThumbnail(vp, td, mat, dkSprites[i], DKfiles[i]);
        }
    }

    public void clickButton(int i) {
        outPlayer.url = i >= 4 ? DKfiles[i - 4] : DSfiles[i];
        if (outPlayer.GetComponent<UiVideoPlayer>() == null)
            outPlayer.gameObject.AddComponent<UiVideoPlayer>();
        MoviePop.SetActive(true);
    }

    public GameObject MoviePop;
    public VideoPlayer outPlayer;
    public Shader mat;
    public RenderTexture td;
    public VideoPlayer vp;
    public DanceManager dm;
}
