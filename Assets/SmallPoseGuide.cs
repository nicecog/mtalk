using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SmallPoseGuide : MonoBehaviour {
    public FlowManager fm;

    public RawImage smallPlate;

    public VideoCaptureCam vcc;
    // Start is called before the first frame update
    void Awake() {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Text txt;
    public Button NextButton;
    private void OnEnable() {
        fm.SmallScale(smallPlate.rectTransform.sizeDelta,smallPlate.rectTransform.anchoredPosition);
        fm.WebCamObject.enabled = true;
        fm.WebCamObject.gameObject.SetActive(true);
        if(name=="LV4Cali")
            fm.WebCamObject.GetComponent<RawImage>().uvRect = new Rect(0.8f,0,-0.6f,1f);
        else
            fm.WebCamObject.GetComponent<RawImage>().uvRect = new Rect(1f,0,-1f,1f);

        NextButton.gameObject.SetActive(false);
        txt.transform.parent.GetComponent<Image>().enabled = true;
        RecordButton.gameObject.SetActive(true);
    }

    public Button RecordButton;
    public void startCount() {
        RecordButton.gameObject.SetActive(false);
        StartCoroutine(waitCount());
    }
    private IEnumerator waitCount() {
        for (int i = 3; i > 0; i--) {
            txt.text = String.Concat(i);
            yield return new WaitForSecondsRealtime(1f);
            
        }

        txt.text = "";
        txt.transform.parent.GetComponent<Image>().enabled = false;
        NextButton.gameObject.SetActive(true);
    }
}
