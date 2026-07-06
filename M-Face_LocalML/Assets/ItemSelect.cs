using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using  UnityEngine.UI;
public class ItemSelect : MonoBehaviour
{

    public Sprite[] ItemList;
    public Image[] Items;
    private List<int> itemsList;
    public Animator[] Effects;
    public GameObject endPop;
    public int sum;
    private void OnEnable() {
        if (userdata != null && userdata.ID != "admin")
            StartCoroutine(SyncProgressThenInit());
        else
            InitItemSelect();
    }

    IEnumerator SyncProgressThenInit() {
        if (userdata != null)
            yield return userdata.SyncProgressFromServerCoroutine();
        InitItemSelect();
    }

    void InitItemSelect() {
        point = 0;
        pointMax = 0;
        LastPage.SetActive(false);
        endPop.SetActive(false);    
        progressCount = 1;
        guageBar.fillAmount = 0.2f;
        guageText.text = "1/5";
        CamStage.SetActive(false);
        int y = sm2.songIdx;
        y = y * 2;
        string str = userdata.ItemString;
        sum = 0;
        for (int i = y; i < y + 2; i++) {
            Rewards[i - y].SetActive(str[i] == 'T');
            if (str[i] == 'T')
                sum++;
        }

        if (sum == 2) {
            LastPage.SetActive(true);
            ac.enabled = false;
            //gameObject.SetActive(false);
            return;
        }
        NextButton.gameObject.SetActive(false);
        selectedIdx = 0;
        foreach (var item in Effects) {
            item.enabled = false;
        }
        
        ac.playNumber(0);
        itemsList = new List<int>();
        for(int i=0;i<ItemList.Length;i++)
            itemsList.Add(i);
        for (int i = 0; i < Items.Length; i++) {
            Items[i].sprite = ItemList[2 * (sm2.songIdx) + i];
        }
        foreach (var item in Items) {
            item.SetNativeSize();
            
            Rect rt = item.sprite.textureRect;
            float m = rt.width > rt.height ? rt.width : rt.height;
            float ratio = 600 / m;
            item.rectTransform.localScale = Vector3.one * ratio;
        }
        
        intro.sprite = introSprite[0];
        intro.SetNativeSize();
        
    }

    public float point;
    public float pointMax;
    public Image intro;
    public Sprite[] introSprite;
    public AudioController ac;
    public bool getItem(int idx) {
        Effects[selectedIdx - 1].enabled = false;
        selectedIdx = 0;
        NextButton.gameObject.SetActive(false);
        if (userdata.ItemString[idx] == 'F') {
            char[] t = new char[17];
            t[16] = '\0';
            for (int i = 0; i < 16; i++)
                t[i] = userdata.ItemString[i];
            t[idx] = 'T';
            userdata.ItemString = new string(t);
            
            userdata.itemChange();
            return true;
        }
        return false;
        
    }
    public void updateItem() {
        int y = sm2.songIdx;
        y = y * 2;
        string str = userdata.ItemString;
        sum = 0;
        for (int i = y; i < y + 2; i++) {
            Rewards[i - y].SetActive(str[i] == 'T');
            if (str[i] == 'T')
                sum++;
        }

        if (sum == Items.Length) {
            LastPage.SetActive(true);
            ac.asc.Stop();
            ac.asc.enabled = false;
            ac.enabled = false;
            //gameObject.SetActive(false);
        }

    }

    public int isDone;
    public GameObject LastPage;
    public Login userdata;
    public int selectedIdx;
    public InstructionChange2 ic2;
    public void selectButton(int idx) {
        if (ac.voiceOn)
            return;
        if (Rewards[idx - 1].activeSelf)
            return;
        if (idx == selectedIdx && selectedIdx > 0) {
            Effects[selectedIdx - 1].enabled = false;
            selectedIdx = 0;
            NextButton.gameObject.SetActive(false);
        } else if (idx != selectedIdx || selectedIdx == 0) {
            if(selectedIdx>0)
                Effects[selectedIdx - 1].enabled = false;
            selectedIdx = idx;
            Effects[idx - 1].enabled = true;
            if(selectedIdx>0)
                NextButton.gameObject.SetActive(true);
        }
    }

    public GameObject[] Rewards;
    public void ClickNext() {
        if (selectedIdx == 0)
            return;
        CamStage.SetActive(true);
        CamStage.transform.GetChild(0).GetComponent<WebCamStage2>().startGame();
    }

    public Stage2Manager sm2;
    public Button NextButton;
    public GameObject CamStage;
    public Image guageBar;
    public Text guageText;
    public int progressCount;
}
