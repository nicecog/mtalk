using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingButton : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake() {
        fm = FindFirstObjectByType<FlowManager>();
        bgs = transform.parent.gameObject.GetComponent<BodyGameScene>();
        bm = FindFirstObjectByType<BodyManager>();
    }

    public AudioSource music;
    public Image panel;

    public void pop() {
        if (!panel.gameObject.activeSelf) {
            panel.gameObject.SetActive(true);
            music.Pause();
        }
    }

    public void exit() {
        music.Play();
        panel.gameObject.SetActive(false);
    }

    public void speedChange() {
        speed.SetActive(true);
        music.Stop();
        bgs.gameObject.SetActive(false);
        panel.gameObject.SetActive(false);
    }

    public void end() {
        bgs.count = 300;
    }

    public FlowManager fm;
    public BodyGameScene bgs;
    public BodyManager bm;
    private void OnEnable() {
        panel.gameObject.SetActive(false);
    }

    public void changeDone() {
        bm.playTimes--;
    }

    public GameObject speed;
    
}
