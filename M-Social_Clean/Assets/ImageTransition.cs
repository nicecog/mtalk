using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageTransition : MonoBehaviour {
    public float mainLength;
    public float transitionLength;
    public Sprite[] sprites;

    public float preCool;
    private void OnEnable() {
        if(sprites.Length>1)
            StartCoroutine(transition());
    }

    public Image self;
    public Image back;
    public int nowIdx;
    public int nextIdx;
    public float timePoint;
    private IEnumerator transition() {
        yield return new WaitForSecondsRealtime(preCool);
        nowIdx = 0;
        nextIdx = 1;
        self.sprite = sprites[nowIdx];
        back.sprite = sprites[nextIdx];
        timePoint = Time.fixedTime;
        while (true) {
            yield return new WaitForEndOfFrame();
            float now = Time.fixedTime;
            if (timePoint + mainLength < now) {
                if (timePoint + mainLength + transitionLength < now) {
                    timePoint = now;
                    nowIdx = (nowIdx + 1) % sprites.Length;
                    nextIdx = (nowIdx + 1) % sprites.Length;
                    self.sprite = sprites[nowIdx];
                    self.color = Color.white;
                    
                    back.sprite = sprites[nextIdx];
                    back.color = Color.clear;
                    
                }
                else {
                    float ratio = (now - timePoint-mainLength) / transitionLength;
                    back.color = new Color(1f,1f,1f,ratio);
                    self.color = new Color(1f,1f,1f,1-ratio);
                }
            }
        }
    }
}
