using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PoseGuide : MonoBehaviour {
    public RawImage HalfCam;

    public VideoCaptureCam vcc;
    public Text txt;
    public Button NextButton;
    private void OnEnable() {
        HalfCam.gameObject.SetActive(true);
        HalfCam.enabled = true;
        NextButton.gameObject.SetActive(false);
        txt.transform.parent.GetComponent<Image>().enabled = true;
        HalfCam.uvRect = new Rect(1f,0,-1f,1f);
        HalfCam.rectTransform.sizeDelta = new Vector2(2800, 1575);
        HalfCam.rectTransform.anchoredPosition = new Vector2(0, -88.5f);
        RecordButton.gameObject.SetActive(true);

    }

    public Button RecordButton;
    public void startCount() {
        vcc.startRecord();
        StartCoroutine(waitCount());
        RecordButton.gameObject.SetActive(false);
    }

    private IEnumerator waitCount() {
        for (int i = 15; i > 0; i--) {
            txt.text = String.Concat(i);
            yield return new WaitForSecondsRealtime(1f);
        }

        txt.text = "";
        txt.transform.parent.GetComponent<Image>().enabled = false;
        vcc.stopRecording(false,1);
        yield return new WaitForSecondsRealtime(0.5f);
        
        NextButton.gameObject.SetActive(true);
    }
    private void OnDisable()
    {
        HalfCam.gameObject.SetActive(false);
    }
}
