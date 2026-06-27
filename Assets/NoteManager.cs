using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoteManager : MonoBehaviour {
    public RectTransform Slice1, Slice2, Slice3, Slice4;

    public float tr1, tr2, tr3;
    // Start is called before the first frame update
    public Transform noteboxHead;
    public Sprite[] noteTypes;
    public FlowManager fm;
    public DanceStarManager dsm;
    public DanceManager dm;
    public BodyManager bm;
    void Awake() {
        fm = FindFirstObjectByType<FlowManager>();
        _Play = false;
        auso = GetComponent<AudioSource>();
    }

    public void startNote() {
        _Play = true;
        //StartCoroutine(createNotes(fm.level==1 ? 480f/90f : fm.level==3 ? 480f/110f : 480f/100f));
        StartCoroutine(createNotes( 480f/100f));
    }

    public bool _Play {
        get;
        set;
        
    }

    private AudioSource auso;
    private void OnEnable() {
        
        startNote();
    }

    public VideoCaptureCam vcc;
    public GameObject nextButton;
    public Text txt;
    public AudioClip[] bgms;
    private IEnumerator createNotes(float term) {
        nextButton.SetActive(false);
        if (dsm.GameLevel > 2) {
            vcc.prefix = "DanceThirdPlay";
            vcc.startRecord();
        }

        tr1 = term*2*1/8f;
        tr2 = term * 2*7f / 8f;
        tr3 = term * 2 * 7f / 16f;
        //tr3 = term * 2.4f;   
        txt.text = MBodyKoreanText.DanceSectionIntro;
        int[] ds = new int[6] {1,4,6,3,0,2};
        int count = 0;
        int[] noteSequence = new int[24]{1,1,1,1,2,2,2,2,1,1,2,2,1,1,1,1,2,2,2,2,1,1,1,1};
        for (int i = 0; i < 24; i++)
            noteSequence[i]--;
        
        txt.text = MBodyKoreanText.DanceSectionPlay;
        {
            GameObject gm = GameObject.Instantiate(Slice1.gameObject, noteboxHead);
            Image img = gm.GetComponent<Image>();
            if (dsm.GameLevel > 2)
                img.sprite = noteTypes[0];
            else if(dsm.GameLevel>1)
                img.sprite = noteTypes[noteSequence[count]];
            else {
                img.sprite = noteTypes[noteSequence[count] +(dm.danceNum - 1) * 2];
                img.SetNativeSize();
                img.rectTransform.sizeDelta = img.rectTransform.sizeDelta * 0.25f;
            }

            img.transform.SetSiblingIndex(0);
            StartCoroutine(noteLife(img));
            count++;
        }
        yield return new WaitForSecondsRealtime(term);
        auso.clip = bgms[ds[(dm.danceNum-1)]];
        auso.Play();
        while (_Play) {
            if(auso.time>=(count-1)*term) {
                GameObject gm = GameObject.Instantiate(Slice1.gameObject, noteboxHead);
                Image img = gm.GetComponent<Image>();
                if (dsm.GameLevel > 2)
                    img.sprite = noteTypes[0];
                else if(dsm.GameLevel>1)
                    img.sprite = noteTypes[noteSequence[count]];
                else {
                    img.sprite = noteTypes[noteSequence[count] +(dm.danceNum - 1) * 2];
                    img.SetNativeSize();
                    img.rectTransform.sizeDelta = img.rectTransform.sizeDelta * 0.25f;
                }

                img.transform.SetSiblingIndex(0);
                StartCoroutine(noteLife(img));
                count++;
                if (count >= 24)
                    break;
            }
            if (!auso.isPlaying) {
                _Play = false;
                yield return new WaitForSecondsRealtime(term+1f);
            }

            yield return null;
        }

        if (dsm.GameLevel > 2) {
            vcc.stopRecording(true);
            int idx = dm.danceNum - 1;
            string temp = dm.danceStar;
            char[] tx = new char[6];
            for (int i = 0; i < 6; i++) {
                tx[i] = temp[i];
            }
            if(tx[idx]=='F')
                tx[idx] = 'T';
            dm.danceStar = String.Concat(tx[0],tx[1],tx[2],tx[3],tx[4],tx[5],temp[6],
                temp[7],temp[8],temp[9],temp[10],temp[11],temp[12],temp[13]);
            dm.fileManager.danceStar = dm.danceStar;
            dm.fileManager.UpdateResult();
            dm.fileManager.WriteResult("Dance,3,"+"0,"+"DanceIndex:"+(dm.danceNum));

        }

        yield return new WaitForSecondsRealtime(term*4);
        _Play = false;
        
        nextButton.SetActive(true);
    }

    private IEnumerator noteLife(Image img) {
        Vector2 original = dsm.GameLevel == 1 ? img.rectTransform.sizeDelta * 0.25f : img.rectTransform.sizeDelta;
        float StartTime = Time.fixedTime;
        while (true) {
            float nowTime = Time.fixedTime;
            float ratio = (nowTime - StartTime) / tr1;
            img.rectTransform.sizeDelta = original*0.25f * (1 - ratio) + original*2f * ratio;
            img.rectTransform.anchoredPosition =
                Slice1.anchoredPosition * (1 - ratio) + Slice2.anchoredPosition * ratio;
            img.color = new Color(1f,1f,1f,ratio);
            if (nowTime >= StartTime + tr1) {
                StartTime = nowTime;
                break;
            }
            if(!_Play)
                Destroy(img.gameObject);
            yield return null;
        }
        while (true) {
            float nowTime = Time.fixedTime;
            float ratio = (nowTime - StartTime) / tr2;
            img.rectTransform.sizeDelta = original*2f * (1 - ratio) + original*6f * ratio;
            img.rectTransform.anchoredPosition =
                Slice2.anchoredPosition * (1 - ratio) + Slice3.anchoredPosition * ratio;
            if (nowTime >= StartTime + tr2) {
                StartTime = nowTime;
                break;
            }

            if(!_Play)
                Destroy(img.gameObject);
            yield return null;
        }
        while (true) {
            float nowTime = Time.fixedTime;
            float ratio = (nowTime - StartTime) / tr3;
            img.rectTransform.sizeDelta = original*6f * (1 - ratio) +original*7.3126f * ratio;
            img.rectTransform.anchoredPosition =
                Slice3.anchoredPosition * (1 - ratio) + Slice4.anchoredPosition * ratio;
            img.color = new Color(1f,1f,1f,1f-0.7f*ratio);
            if (nowTime >= StartTime + tr3) {
                StartTime = nowTime;
                break;
            }
            if(!_Play)
                Destroy(img.gameObject);
            yield return null;
        }

        Destroy(img.gameObject);
    }
    
    private void OnDisable() {
        _Play = false;
        StopAllCoroutines();
        for (int i = noteboxHead.childCount;i>0; i--) {
            Destroy(noteboxHead.GetChild(i - 1).gameObject);
        }
    }
}
