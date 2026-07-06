using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InstructionChange : MonoBehaviour
{
    public Image img;
    public Image sub;
    public Sprite[] mainSprites;
    public Sprite[] subSprites;
    public Stage1Manager sm1;
    //public Stage2Manager sm2;
    public void change(string msg) {
        sub.enabled = false;
        if (msg == "Stage1Select") {
            img.sprite = mainSprites[0];
        } else if (msg.Contains("P8")) {
            if (msg.Contains("Happy"))
                img.sprite = mainSprites[1];
            else if (msg.Contains("Neutral"))
                img.sprite = mainSprites[2];
            else if (msg.Contains("Sad"))
                img.sprite = mainSprites[3];
            else
                img.sprite = mainSprites[4];
        } else if (msg.Contains("Incorrect")) {
            sub.enabled = true;
            if (msg.Contains("IncorrectHappy")) {
                img.sprite = mainSprites[6];
                ac1 = 0;
            }
            else if (msg.Contains("IncorrectNeutral")) {
                img.sprite = mainSprites[7];
                ac1 = 1;
            }
            else if (msg.Contains("IncorrectSad")) {
                img.sprite = mainSprites[8];
                ac1 = 2;
            }
            else {
                img.sprite = mainSprites[9];
                ac1 = 3;
            }


            if (msg.Contains("SubHappy")) {
                sub.sprite = subSprites[0];
                ac2 = 4;
            }
            else if (msg.Contains("SubNeutral")) {
                ac2 = 5;
                sub.sprite = subSprites[1];
            }
            else if (msg.Contains("SubSad")) {
                sub.sprite = subSprites[2];
                ac2 = 6;
            }
            else{
                sub.sprite = subSprites[3];
                ac2 = 7;
            }

            StartCoroutine(SoundEffect());
        }
        else if (msg.Contains("P9"))
            img.sprite = mainSprites[5];
        else if (msg.Contains("P15")) {
            if (msg.Contains("Happy"))
                img.sprite = mainSprites[10];
            else if (msg.Contains("Neutral"))
                img.sprite = mainSprites[11];
            else if (msg.Contains("Sad"))
                img.sprite = mainSprites[12];
            else
                img.sprite = mainSprites[13];
        }
        
        sub.SetNativeSize();
        img.SetNativeSize();
    }
    
    private int ac1, ac2;
    public Sprite alpha;
    IEnumerator SoundEffect() {
        AudioController ac = GetComponent<AudioController>();
        ac.playNumber(ac1);
        while (ac.asc.isPlaying)
            yield return null;
        yield return new WaitForSeconds(0.3f);
        ac.playNumber(ac2);
        yield return new WaitForSeconds(0.3f);
        while (ac.asc.isPlaying)
            yield return null;
        yield return new WaitForSeconds(0.3f);
        sm1.con.playClip2(sm1.ac);
    }
    private void OnEnable() {
        
    }
}
