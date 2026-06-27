using System;
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

    public IEnumerator prefare() {
        thirds = new List<string>();
        string[] t = Directory.GetFiles(MBodyPaths.DataRoot);
        for (int i = t.Length - 1; i >= 0; i--) {
            if (t[i].Contains(dm.ID) && !t[i].Contains(".meta") && t[i].Contains("BodyThird"))
                thirds.Add(t[i]);
        }

        for (int i = 0; i < 8 && i < thirds.Count; i++) {
            Debug.Log(thirds[i]);
            yield return VideoThumbnailGenerator.CaptureButtonThumbnail(vp, td, mat, dsSprites[i], thirds[i]);
        }

        yield return null;
    }

    public void clickButton(int i) {
        outPlayer.url = thirds[i];
        if (outPlayer.GetComponent<UiVideoPlayer>() == null)
            outPlayer.gameObject.AddComponent<UiVideoPlayer>();
        MoviePop.SetActive(true);
    }

    public Button[] dsSprites;
    public GameObject MoviePop;
    public VideoPlayer outPlayer;
    public Shader mat;
    public RenderTexture td;
}
