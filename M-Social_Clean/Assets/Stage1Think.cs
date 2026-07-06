using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Stage1Think : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public AudioClip[] clips;
    private void OnEnable() {
        nowScene = 0;
        btns[0].gameObject.SetActive(true);
        btns[1].gameObject.SetActive(false);
        asc.loop = false;
        img.sprite = pages[0];
        next.gameObject.SetActive(false);
        asc.playOnAwake = false;
        asc.clip = clips[0];
        asc.Play();
    }

    public Image img;
    public Sprite[] pages;
    public int nowScene = 0;
    public AudioSource asc;
    public Button[] btns;
    public void clickRight(int idx) {
        if (asc.isPlaying||idx<=nowScene)
            return;

        if (idx == 1) {
            //img.sprite = pages[0];
            StartCoroutine(B1());
        }
        else if(idx==2) {
            img.sprite = pages[2];
            StartCoroutine(B2());
        }
    }

    IEnumerator B1() {
        asc.clip = clips[1];
        asc.Play();
        yield return new WaitForSeconds(1f);
        while(asc.isPlaying)
            yield return new WaitForSeconds(0.2f);
        btns[1].gameObject.SetActive(true);
        btns[0].gameObject.SetActive(false);
        img.sprite = pages[1];

    }

    IEnumerator B2() {
        btns[1].gameObject.SetActive(false);
        asc.clip = clips[2];
        asc.Play();
        yield return new WaitForSeconds(1f);
        while(asc.isPlaying)
            yield return new WaitForSeconds(0.2f);
        next.gameObject.SetActive(true);
    }

    public Button next;

    public void ClickNextButton() {
        gameObject.SetActive(false);
        s1m.ProgressDone();
    }

    public Stage1Manager s1m;
}
