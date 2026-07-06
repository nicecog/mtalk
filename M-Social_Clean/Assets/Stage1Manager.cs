using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

public class Stage1Manager : MonoBehaviour {

    public Sprite[] SelectSprites;
    public Text[] LevelCounts;
    public int[] sceneLeft;
    public Image selectGlow;
    public Button confirm;
    public Image[] cards;
    public int progressCount;
    
    public Login userdata;
    public AudioController con;
    public List<int> scenesDone;
    private void OnEnable() {
        if (userdata == null)
            userdata = MSocialAppServices.Instance != null ? MSocialAppServices.Instance.UserData : null;
        if (userdata == null)
            userdata = FindFirstObjectByType<Login>(FindObjectsInactive.Include);
        if (userdata == null) {
            MSocialStartupLog.Log("Stage1Manager.OnEnable missing userdata");
            return;
        }

        if (Stage2Intro == null)
            Stage2Intro = MSocialPageRegistry.Get(MSocialPageId.Stage2Intro);
        if (Stage2Intro != null)
            Stage2Intro.SetActive(false);
        if (LastPop != null)
            LastPop.SetActive(false);
        con = GetComponent<AudioController>();
        progress = 0;
        progressCount = userdata.playNum;
        selectedCard = 0;
        confirm.interactable = false;
        selectGlow.gameObject.SetActive(false);
        sceneselect.gameObject.SetActive(false);
        string stage1Log = userdata.Stage1Progress;
        scenesDone = new List<int>();
        int[] maximum = userdata.SceneMaximum;
        sceneLeft = new int[6];
        for (int i = 0; i < 6; i++) {
            sceneLeft[i] = maximum[i];
            //sceneLeft[i] = sceneLeft[i] > 2 ? 2 : sceneLeft[i];
            LevelCounts[i].text = sceneLeft[i].ToString("0");
            LevelCounts[i].transform.parent.gameObject.SetActive(sceneLeft[i]>0);
        }
    }
    

    public int selectedCard;
    public void ClickCard(int select) {
        //if (con.voiceOn||sceneLeft[select-1]<1)
        //    return;
        if (selectedCard == 0 || selectedCard != select) {
            selectedCard = select;
            selectGlow.gameObject.SetActive(true);
            selectGlow.rectTransform.position = cards[selectedCard - 1].rectTransform.position;
            selectGlow.sprite = SelectSprites[selectedCard - 1];
            selectGlow.SetNativeSize();
            
        } else if (selectedCard == select) {
            selectedCard = 0;
            selectGlow.gameObject.SetActive(false);
        }

        confirm.interactable = (selectedCard > 0);
    }

    public void ExitApplication() {
        Application.Quit();
    }
    public void ProgressDone() {
        fbm.gameObject.SetActive(false);
        progress++;
        if (progress == 4) {
            LastPop.SetActive(true);
            scenesDone.Add(player.SceneIndex);
        }
        else if (progress>=5) {
            LastPop.SetActive(false);
            sceneselect.Stage1MainScene.SetActive(false);
            sceneselect.gameObject.SetActive(false);
            StartCoroutine(EnterStage2());
        }
        else {
            sceneselect.Stage1MainScene.SetActive(false);
            sceneselect.gameObject.SetActive(false);
            scenesDone.Add(player.SceneIndex);
        }
    }

    public GameObject LastPop;
    public GameObject Stage2Intro;
    public int progress;
    public void ClickConfirm() {
        con.asc.Stop();
        sceneselect.sceneCategory = selectedCard;
        sceneselect.SceneLeft = sceneLeft[selectedCard - 1];
        ClickCard(selectedCard);
        sceneselect.gameObject.SetActive(true);
    }

    public ScenePlayer player;
    public SceneSelect sceneselect;
    public Stage1FeedbackManager fbm;

    IEnumerator EnterStage2() {
        var host = (MonoBehaviour)MSocialAppServices.EnsureExists();
        yield return MSocialSceneRouter.EnsureLoaded(MSocialSceneId.Stage2);
        yield return MSocialSceneRouter.ShowPage(MSocialPageId.Stage2Intro, host);
        yield return null;
        yield return MSocialSceneRouter.Unload(MSocialSceneId.Stage1);
    }
}
