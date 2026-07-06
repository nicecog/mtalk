using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Stage1Positive : MonoBehaviour
{
    // Start is called before the first frame update
    public int mainEmotion;
    public int selectedEmotion;
    
    void Start()
    {
        
    }

    public Sprite[] message;
    
    public Sprite[] face;
    public Image messageTarget;
    public Image faceTarget;
    private void OnEnable() {
        faceTarget.sprite = face[selectedEmotion];
        
        messageTarget.sprite = message[mainEmotion];
        messageTarget.SetNativeSize();
        con.playNumber(mainEmotion);
    }

    public AudioController con;
        // Update is called once per frame
    void Update()
    {
        
    }

    public Button NextButton;

    public void ClickNextButton() {
        if (con.voiceOn)
            return;
        sm.ProgressDone();
        gameObject.SetActive(false);
    }

    public Stage1Manager sm;
}
