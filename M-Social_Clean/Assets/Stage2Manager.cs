using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Stage2Manager : MonoBehaviour {
    public Button[] buttons;
    public void skipped() {
        s1m.scenesDone = new List<int>();
        int sum = 0;
        foreach (var item in s1m.userdata.SceneMaximum)
            sum += item;
        for (int i = 0; i < sum; i++) {
            s1m.scenesDone.Add(i);
        }   
        Debug.Log(sum);

        s1m.scenesDone = ShuffleList(s1m.scenesDone);
        for (int i = sum-1; i >3; i--) {
            s1m.scenesDone.RemoveAt(i);
        }
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

    public ScenePlayer sp;
    private void OnEnable() {
        tt = sp.titles;
        asc = GetComponent<AudioSource>();
        list = s1m.scenesDone;
        scenesToDo = -1;
        Vertical.text = "";
        title.text = "";
        Select.enabled = false;
        NextButton.interactable = false;
        for (int i = 0; i < 4; i++) {
            buttons[i].image.sprite = Icons[list[i]];
        }

        nowIndex = 0;
        mainScreen.sprite = alpha;
    }

    public int nowIndex;
    public List<int> list;
    public Login userdata;
    public int scenesToDo = 0;
    public Sprite[] FirstFrames;
    public Image mainScreen;


    public Stage1Manager s1m;
    private AudioSource asc;
    public void optionClicked(int idx) {
        if (idx == 0) {
            scenesToDo = -1;
            mainScreen.sprite = alpha;
            Vertical.text = "";
            Select.enabled = false;
            NextButton.interactable = false;
            title.text = "";
            return;
        }
        if (list[idx - 1] == scenesToDo||asc.isPlaying) {
            scenesToDo = -1;
            mainScreen.sprite = alpha;
            Vertical.text = "";
            Select.enabled = false;
            NextButton.interactable = false;
            title.text = "";
        }
        else {
            mainScreen.sprite = FirstFrames[list[idx - 1]];
            nowIndex = idx;
            scenesToDo = list[idx - 1];
            Vertical.text = vert[scenesToDo];
            Select.enabled = true;
            Select.rectTransform.anchoredPosition = buttons[idx - 1].image.rectTransform.anchoredPosition;
            NextButton.interactable = true;
            title.text = tt[scenesToDo];
        }
    }

    public Text title;
    private String[] tt;
    public Image Select;
    public Sprite[] Icons;
    private String[] vert = {
        "식\n판","반\n지","마\n이\n크","자\n전\n거","휴\n대\n용\n게\n임\n기","아\n이\n스\n크\n림","버\n스","경\n적",
        
        "포\n장\n된\n선\n물",
        "인\n사",
        "약\n속",
        "타\n이\n머",
        "카\n드\n놀\n이",
        "장\n바\n구\n니",
        "햄\n버\n거",
        "호\n랑\n이",

        
        "작\n은\n눈",
        "책\n방",
        "청\n소\n기",
        "케\n이\n크",
        "태\n블\n릿",
        "피\n구\n공",
        "수\n족\n관",
        "휴\n대\n폰",

        "낙\n서",
        "골\n든\n마\n이\n크",
        "가\n방",
        "엄\n지\n척",
        "악\n보",
        "입\n술\n에\n손\n가\n락",
        "잡\n담\n금\n지",
        "집"
    };

    public Sprite alpha;
    public void Confirm() {
        if (scenesToDo == -1||asc.isPlaying)
            return;
        WatchScene.gameObject.SetActive(true);
        Vertical.text = "";
        title.text = "";
        Select.enabled = false;
        NextButton.interactable = false;
        buttons[nowIndex - 1].interactable = false;
        
    }

    public Button NextButton;
    public Stage2ScenePlay WatchScene;
    public Text Vertical;
}
