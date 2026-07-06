using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Stage1Negative : MonoBehaviour
{
    // Start is called before the first frame update
    public int mainEmotion;
    void Start()
    {
        
    }
    private List<T> ShuffleList<T>(List<T> list)
    {
        int random1,  random2;
        T temp;

        for (int i = 0; i < list.Count; ++i)
        {
            random1 = UnityEngine.Random.Range(0, list.Count);
            random2 = UnityEngine.Random.Range(0, list.Count);

            temp = list[random1];
            list[random1] = list[random2];
            list[random2] = temp;
        }

        return list;
    }

    public Login userdata;
    public Sprite[] message;
    public Sprite[] Contents;
    public Sprite[] face;
    public Image messageTarget;
    public Image faceTarget;
    private void OnEnable() {
        if (userdata == null)
            userdata = MSocialAppServices.Instance != null ? MSocialAppServices.Instance.UserData : null;
        if (userdata == null)
            userdata = FindFirstObjectByType<Login>(FindObjectsInactive.Include);

        fbm.gameObject.SetActive(false);
        faceTarget.sprite = face[mainEmotion];
        SelectGlow.gameObject.SetActive(false);
        messageTarget.sprite = message[mainEmotion];
        messageTarget.SetNativeSize();
        con.playNumber(mainEmotion);
        selectObject.SetActive(false);
        NextButton.gameObject.SetActive(false);
        optionList = new List<int>();
        int temp = codes[mainEmotion];
        for (int i = 0; i < 5; i++) {
            optionList.Add(temp % 10);
            temp /= 10;
        }

        optionList = ShuffleList(optionList);
        
        //optionList.RemoveAt(0);        
        //optionList.RemoveAt(0);
        SelectButtons[0].sprite = Contents[optionList[0]-1];
        SelectButtons[1].sprite = Contents[optionList[1]-1];
        SelectButtons[2].sprite = Contents[optionList[2]-1];
        StartCoroutine(showOption());
    }

    private List<int> optionList;
    public int[] codes;

    IEnumerator showOption() {
        yield return new WaitForSeconds(1f);
        while (con.voiceOn)
            yield return null;
        selectObject.SetActive(true);
    }
    public void SelectOption(int idx) {
        if (con.voiceOn)
            return;
        if (idx == selectIndex) {
            NextButton.gameObject.SetActive(false);
            selectIndex = 0;
            SelectGlow.gameObject.SetActive(false);
        }
        else {
            NextButton.gameObject.SetActive(true);
            SelectGlow.gameObject.SetActive(true);
            selectIndex = idx;
            SelectGlow.rectTransform.anchoredPosition = (SelectButtons[idx - 1].rectTransform.anchoredPosition) - new Vector2(10,0);
            
        }
    }

    public Image[] SelectButtons;
    public Image SelectGlow;
    public int selectIndex;
    public GameObject selectObject;
    public AudioController con;
    // Update is called once per frame
    void Update()
    {
        
    }

    public Button NextButton;

    public void ClickNextButton() {
        if (selectIndex < 1 || fbm == null) {
            MSocialStartupLog.Log("Stage1Negative next blocked select=" + selectIndex + " fbm=" + (fbm != null));
            return;
        }
        if (con != null && con.voiceOn) {
            MSocialStartupLog.Log("Stage1Negative next blocked voiceOn");
            return;
        }

        int t = optionList[selectIndex - 1];
        if (t < 1 || t > 8) {
            MSocialStartupLog.Log("Stage1Negative invalid task=" + t);
            return;
        }

        String task = t==1?"CountNumber":
                t==2?"Imagine":
                t==3?"Dance":
                t==4?"PlayInstrument":
                t==5?"Sing":
                t==6?"ListenMusic":
                t==7?"Think":
                t==8?"Breath":
                "others";
        if (userdata != null)
            userdata.logUpload("{Stage:Stage1Training;Task:"+task+"}");

        MSocialStartupLog.Log("Stage1Negative -> feedback type=" + (t - 1) + " task=" + task);
        HideWatchScene();
        fbm.startFeedback(t - 1);
        fbm.gameObject.SetActive(true);
        gameObject.SetActive(false);
    }

    void HideWatchScene() {
        if (sm == null)
            return;
        if (sm.player != null && sm.player.es != null)
            sm.player.es.gameObject.SetActive(false);
        if (sm.sceneselect != null && sm.sceneselect.Stage1MainScene != null)
            sm.sceneselect.Stage1MainScene.SetActive(false);
    }

    public Stage1FeedbackManager fbm;
    public Stage1Manager sm;
}


