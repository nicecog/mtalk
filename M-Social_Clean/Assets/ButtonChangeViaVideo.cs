using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class ButtonChangeViaVideo : MonoBehaviour {
    public Sprite skip;
    private Image img;
    public Sprite next;
    // Start is called before the first frame update
    void Awake() {
        vp = transform.parent.GetComponent<VideoPlayer>();
        img = GetComponent<Image>();
        isChanged = false;
    }

    private bool isChanged;
    private void OnEnable() {
        img.sprite = skip;
        img.SetNativeSize();
        isChanged = false;
        startTime = Time.fixedTime;
    }

    private float startTime;

    private VideoPlayer vp;

    // Update is called once per frame
    void Update()
    {
        if (!isChanged) {
            if (Time.fixedTime >= startTime + 5f) {
                if (!vp.isPlaying) {
                    img.sprite = next;
                    img.SetNativeSize();
                    isChanged = true;
                }
            }
        }
    }
}
