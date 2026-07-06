using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class DancePractice : MonoBehaviour {
    public Image frame;

    private float lastPlayTime;
    // Update is called once per frame
    void Update()
    {
        if (!vp.isPlaying && isPlaying&&Time.fixedTime>lastPlayTime+3f) {
            isPlaying = false;
            playButton.gameObject.SetActive(true);
            endButtons.SetActive(true);
        }
    }

    private bool isPlaying = false;
    public Sprite soft;
    public Sprite power;
    public void ChangeMode(bool isSoft) {
        int dn = danceNum;
        if (isSoft) 
            frame.sprite = soft;
        else 
            frame.sprite = power;
        if (dm.level == 1) {
            TopText.text = MBodyKoreanText.DancePracticeModeText(dn, isSoft);
        }

        int plusOne = isSoft ? 0 : 1;
        vp.Stop();
        vp.clip = clips[(danceNum - 1) * 2 + plusOne];
        playButton.gameObject.SetActive(true);
        endButtons.SetActive(false);
        isPlaying = false;
    }

    public DanceManager dm;
    public Text TopText;
    public Button playButton;
    public GameObject endButtons;
    private void OnEnable() {
        BodyResourceLifecycle.ReleaseBodyCapture();
        vp = GetComponent<VideoPlayer>();
        FlowManager fm = FindFirstObjectByType<FlowManager>();
        fm.setBackground(5);
        fm.setBlackScreen(false);
        dm = FindFirstObjectByType<DanceManager>();
        danceNum = dm.danceNum;
        ChangeMode(false);
    }

    public int danceNum = 0;
    
    private VideoPlayer vp;

    public void PlaySource() {
        RestartVideo();
        isPlaying = true;
        endButtons.gameObject.SetActive(false);
        playButton.gameObject.SetActive(false);
        lastPlayTime = Time.fixedTime;
    }

    public VideoClip[] clips;

    void RestartVideo()
    {
        var ui = vp.GetComponent<UiVideoPlayer>();
        if (ui != null)
            ui.RestartPlayback();
        else
            vp.Play();
    }
}
