using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

public class latestVideoPlay : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake() {
        vcc = FindFirstObjectByType<VideoCaptureCam>();
        vp = GetComponent<VideoPlayer>();
    }

    public VideoPlayer vp;
    public VideoCaptureCam vcc;

    private void OnEnable() {
        vp.url = vcc.latestMovie;
        MBodyDiagLog.Step("LatestVideo", $"OnEnable url={vp.url}");
        if (ScenePageVideoDriver.Instance != null)
            ScenePageVideoDriver.Instance.PlayPlayer(vp);
        else
            vp.Play();
    }
}
