using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public class ActSelect : MonoBehaviour
{
    private void Awake() {
        progress = 0;
        ac.asc.volume = 1f;
    }

    public void selectCloud(int idx) {
        if (ac.voiceOn || ac.asc.isPlaying || (s2m.scenesToDo == 5 && idx == 3)) return;
        if (idx == nowIndex) {
            NextButton.interactable = false;
            for (int i = 0; i < 3; i++)
                Clouds[i].image.sprite = cloudSprites[i];
            nowIndex = 0;
            script.text = "";
        }
        else {
            NextButton.interactable = true;
            nowIndex = idx;
            ac.asc.clip = nowClips[idx-1];
            ac.asc.Play();
            for (int i = 0; i < 3; i++)
                Clouds[i].image.sprite = cloudSprites[i];
            Clouds[idx - 1].image.sprite = cloudSprites[idx - 1 + 3];
            Positive_Result.clips[0] = nowClips[idx + 2];
            Negative_Result.clips[0] = nowClips[idx + 2];
            PositiveText.text = nowInstructions[idx - 1];
            NegativeText.text = nowInstructions[idx - 1];
            script.text = answerScripts[3 * (s2m.scenesToDo) + idx - 1];
        }
        
    }

    public AudioController Positive_Result, Negative_Result;
    public String[] answerScripts = { };
    public String[] instructions= {
        "네, 이렇게 상대방에게 무엇을 잘못했는지 말할 수는 있어요. 하지만 더 좋은 방법 도 있으니 다른 선택을 해보세요",
        "하하, 맞아요! 무엇을 잘못 했는지 지적하기 보다는 해결할 수 있는 방법이나 부탁을 하듯 말하는 것이 더 좋은 방법이에요. ",
        "이런~ 정답이 아니에요. 가만히 있는다고 문제가 해결 되지는 않지요. 다시 선택해 보세요.",
        "이런, 정답이 아니에요. 친구가 속상해 하고 있는데 친구의 마음을 알아주거나 도움을 주면 어떨까요? 다시 선택해 보세요.",
        "맞아요, 상대방의 마음을 잘 읽어주고 도와주려고 한 좋은 답변입니다. ",
        "이런, 정답이 아니에요. 바쁘지 않다면 같이 도와주면 좋겠지만, 급한 일이 있다면 왜 도움을 줄 수 없는지 먼저 설명해 주는것이 좋지 않을까요? 다시 선택해 보세요.",
        "이런, 정답이 아니에요. 실수 할 때마다 앞으로 아무것도 하지 않겠다고 생각하면 과연 무엇을 할 수 있을까요? 다시 선택해보세요.",
        "맞아요. 사람은 누구나 실수를 할 수 있으니 너무 속상해하지 않아도 된답니다.",
        "맞아요. 다음번에 더 잘할 수 있도록 노력하는 것은 좋은 방법이에요. 하지만 누구나 실수를 할 수 있다는 것을 아는 것도 중요하답니다!",
        "이런, 정답이 아니에요. 이건 부탁이 아니라 명령하는 것 처럼 들리네요. 다시 선택해 보세요.",
        "이런, 정답이 아니에요. 친한 친구라 하더라도 먼저 물어본 후에 만져보는 것이 좋아요. 다시 선택해 보세요.",
        "맞아요, 남의 물건을 만져보기 전에 늘 주인의 허락을 먼저 구해야 한답니다. 그리고 부탁을 거절하더라도 친구의 마음을 존중해 주어야 해요. ",
        "이런, 정답이 아니에요. 게임이 중요한지 친구가 중요한지 잘 생각해보면 좋겠어요. 다시 선택해 보세요.",
        "맞아요. 친구와 함께 있을 때는  혼자만 할 수 있는 놀이를 하면 안되죠. 그리고 말을 잘 못들었으면 사과를 하고 다시 얘기해 줄 것을 부탁하는 것이 좋습니다. ",
        "이런, 정답이 아니에요. 친구의 표정을 보고 친구가 원하는 것이 무엇일지 잘 생각해 보고 다시 선택해 보세요.",
        "이런, 정답이 아니에요. 그냥 두고 가면 여러 사람이 아이스크림을 밟고 더 더러워지거나 미끄러져 다칠 수도 있어요. 다시 선택해 보세요.",
        "훌륭한 선택이에요. 직접 해결해 보는것도 좋겠지만 어려운 상황에서는 도움을 요청하는 것도 좋은 방법이에요. ",
        "훌륭한 선택이에요. 직접 해결해 보는것도 좋겠지만 어려운 상황에서는 도움을 요청하는 것도 좋은 방법이에요. ",
        "이런, 정답이 아니예요. 너무 긴 시간이 아니더라도 아무 말 없이 약속에 늦게되면 친구가 걱정할 수 있으니 상황을 얘기해 주는 것이 좋아요. 다시 선택해 보세요. ",
        "맞아요. 약속 시간에 조금 늦게 된다면 친구에게 상황을 설명해 주고 조금 기다려 달라고 부탁하면 되요. 친구가 20분 기다리는게 힘들 것 같다면 이야기 해줄거에요. 잘 선택했어요! ",
        "가끔 약속시간에 늦게 될 수도 있어요. 바로 약속을 취소하기 보다는 상황을 설명하고 시간을 조정해 보면 어떨까요? 다른 선택을 해보세요.",
        "이런, 정답이 아니에요. 갑자기 큰소리로 \"실례합니다 비켜주세요\"라고 외친다면 다른 사람들이 놀랄 수도 있어요. 다시 선택해보세요.",
        "맞아요. 사람들이 많은 곳에서는 \"실례합니다\"라고 이야기한 뒤 길을 비켜주면 걸어가는 게 좋아요.",
        "이런, 정답이 아니에요. 아무말 없이 사람들이 나갈 때까지 가만히 서있는 것은 좋은 방법이 아니에요. 다시 선택해보세요."    
    };

    public Text PositiveText;
    public Text NegativeText;
    public string[] nowInstructions;
    public int[] emotions;
    public int[] answerIndex;
    private void OnEnable() {
        Positive.SetActive(false);
        PositiveF.SetActive(false);
        Negative.SetActive(false);
        NextButton.interactable = false;
        popac = Popup.GetComponent<AudioController>();
        Popup.gameObject.SetActive(false);
        int temp = s2m.scenesToDo;
        nowClips = new AudioClip[6];
        nowInstructions = new string[3];
        for (int i = 0; i < 6; i++)
            nowClips[i] = allclip[temp*6+i];
        emotions = new int[temp==5?2:3];
        Clouds[2].gameObject.SetActive(temp!=5);
        for (int i = 0; i < 3; i++) {
            Clouds[i].image.sprite = cloudSprites[i];
            Clouds[i].interactable = true;
        }

        for (int i = 0; i < 3; i++) {
            nowInstructions[i] = instructions[temp * 3 + i];
        }

        nowIndex = 0;
        switch (temp) {
            case 0:
                emotions[0] = 0;
                emotions[1] = 1;
                emotions[2] = 1;
                answerIndex = new int[1] {1};
                break;
            case 1:
                emotions[0] = 2;
                emotions[1] = 3;
                emotions[2] = 3;
                answerIndex = new int[1] {1};
                break;
            case 2:
                emotions[0] = 0;
                emotions[1] = 1;
                emotions[2] = 4;
                answerIndex = new int[2] {1,2};
                break;
            case 3:
                emotions[0] = 2;
                emotions[1] = 1;
                emotions[2] = 5;
                answerIndex = new int[1] {2};
                break;
            case 4:
                emotions[0] = 1;
                emotions[1] = 3;
                emotions[2] = 5;
                answerIndex = new int[1] {1};
                break;
            case 5:
                emotions[0] = 6;
                emotions[1] = 1;
                answerIndex = new int[1] {1};
                break;
            case 6:
                emotions[0] = 1;
                emotions[1] = 3;
                emotions[2] = 1;
                answerIndex = new int[1] {1};
                break;
            case 7:
                emotions[0] = 1;
                emotions[1] = 1;
                emotions[2] = 4;
                answerIndex = new int[1] {1};
                break;
            case 8:
                emotions[0] = 1;
                emotions[1] = 1;
                emotions[2] = 4;
                answerIndex = new int[1] {2};
                break;
            case 9:
                emotions[0] = 2;
                emotions[1] = 2;
                emotions[2] = 5;
                answerIndex = new int[2] {0,1};
                break;
            case 10:
                emotions[0] = 1;
                emotions[1] = 3;
                emotions[2] = 1;
                answerIndex = new int[2] {1,2};
                break;
            case 11:
                emotions[0] = 1;
                emotions[1] = 0;
                emotions[2] = 1;
                answerIndex = new int[1] {2};
                break;
            case 12:
                emotions[0] = 5;
                emotions[1] = 1;
                emotions[2] = 5;
                answerIndex = new int[1] {0};
                break;
            case 13:
                emotions[0] = 1;
                emotions[1] = 5;
                emotions[2] = 5;
                answerIndex = new int[1] {1};
                break;
            case 14:
                emotions[0] = 1;
                emotions[1] = 0;
                emotions[2] = 1;
                answerIndex = new int[1] {2};
                break;
            case 15:
                emotions[0] = 3;
                emotions[1] = 2;
                emotions[2] = 1;
                answerIndex = new int[2] {1,2};
                break;
            case 16:
                emotions[0] = 0;
                emotions[1] = 2;
                emotions[2] = 1;
                answerIndex = new int[2] {0,2};
                break;
            case 17:
                emotions[0] = 0;
                emotions[1] = 6;
                emotions[2] = 1;
                answerIndex = new int[1] {2};
                break;
            case 18:
                emotions[0] = 6;
                emotions[1] = 3;
                emotions[2] = 0;
                answerIndex = new int[1] {1};
                break;
            case 19:
                emotions[0] = 0;
                emotions[1] = 1;
                emotions[2] = 2;
                answerIndex = new int[2] {1,2};
                break;
            case 20:
                emotions[0] = 2;
                emotions[1] = 0;
                emotions[2] = 1;
                answerIndex = new int[2] {0,2};
                break;
            case 21:
                emotions[0] = 1;
                emotions[1] = 2;
                emotions[2] = 0;
                answerIndex = new int[2] {0,1};
                break;
            case 22:
                emotions[0] = 1;
                emotions[1] = 5;
                emotions[2] = 9;
                answerIndex = new int[2] {1,2};
                break;
            case 23:
                emotions[0] = 6;
                emotions[1] = 3;
                emotions[2] = 1;
                answerIndex = new int[1] {1};
                break;
            case 24:
                emotions[0] = 2;
                emotions[1] = 1;
                emotions[2] = 0;
                answerIndex = new int[2] {1,2};
                break;
            case 25:
                emotions[0] = 0;
                emotions[1] = 3;
                emotions[2] = 1;
                answerIndex = new int[2] {1,2};
                break;
            case 26:
                emotions[0] = 6;
                emotions[1] = 1;
                emotions[2] = 5;
                answerIndex = new int[2] {1,2};
                break;
            case 27:
                emotions[0] = 7;
                emotions[1] = 5;
                emotions[2] = 1;
                answerIndex = new int[1] {1};
                break;
            case 28:
                emotions[0] = 0;
                emotions[1] = 1;
                emotions[2] = 2;
                answerIndex = new int[2] {1,2};
                break;
            case 29:
                emotions[0] = 1;
                emotions[1] = 0;
                emotions[2] = 8;
                answerIndex = new int[1] {2};
                break;
            case 30:
                emotions[0] = 2;
                emotions[1] = 0;
                emotions[2] = 1;
                answerIndex = new int[1] {2};
                break;
            case 31:
                emotions[0] = 3;
                emotions[1] = 1;
                emotions[2] = 2;
                answerIndex = new int[1] {2};
                break;
        }

        for (int i = 0; i < (temp==5?2:3); i++) {
            maskedImage[i].sprite = emotionIcons[emotions[i]];
            maskedImage[i].SetNativeSize();
            
        }

        if (temp == 5) //0257
            maskedImage[2].sprite = alpha;
        script.text = "";
    }
    private String[] emotionText = {"littleAngry","BlankFace","littleSmile","littleSad","Comfortable","Smile","Angry","Surprised","Sad","LittleSurprised"};
    public Sprite alpha;
    public Image[] maskedImage;
    public Sprite[] cloudSprites;
    private AudioClip[] nowClips;
    public int nowIndex;
    public Button NextButton;

    public void confirm() {
        if (nowIndex < 1||ac.voiceOn||ac.asc.isPlaying) return;
        //s1m.player.SceneEmotions
        Popup.gameObject.SetActive(true);
        if (answerIndex.Contains(nowIndex - 1)) {
            int temp = UnityEngine.Random.Range(0, 4);
            
            popac.playNumber(temp);
            Popup.sprite = PopupSprites[temp];
        }
        else {
            /*
            Popup.sprite = PopupSprites[1];
            popac.playNumber(1);
            */
            Clouds[nowIndex - 1].interactable = false;
            PopupEnd();
            for (int i = 0; i < 3; i++)
                Clouds[i].image.sprite = cloudSprites[i];
                
        }
            
        //Popup.GetComponent<AudioController>().playNumber();
    }

    public Text script;
    private AudioController popac;
    public void PopupEnd() {
        if (popac.voiceOn) return;
        Popup.gameObject.SetActive(false);
        bool isCorrect = answerIndex.Contains(nowIndex - 1);
        userdata.logUpload("{Stage:ActGood;Progress:("+(progress+1) +"/4);Scene:"+(s2m.scenesToDo+1)+";"+
                           "Select:"+nowIndex+";SelectedFaceEmotion:"+emotionText[emotions[nowIndex-1]]+";isCorrect:"+(isCorrect?"Correct":"Incorrect") +"}");
        if (isCorrect) {
            Positive.SetActive(true);
        }
        else {
            Negative.SetActive(true);
            nowIndex = 0;
        }
    }

    public void PositiveEnd() {
        
        AudioController peac = PositiveF.GetComponent<AudioController>();
        if (peac.voiceOn) return;
        progress++;
        if (progress >= 4) {
            endPage.SetActive(true);
        }
        else {
            Positive.gameObject.SetActive(false);
            Negative.gameObject.SetActive(false);
            PositiveF.gameObject.SetActive(false);
            gameObject.SetActive(false);
            s2m.WatchScene.gameObject.SetActive(false);
        }
    }
    public int progress;
    public GameObject Positive, Negative, PositiveF, endPage;
    public AudioController ac;
    public Sprite[] emotionIcons;
    public Stage2Manager s2m;
    public Button[] Clouds;
    public Image Popup;
    public Sprite[] PopupSprites;
    public AudioClip[] allclip;
    public Stage1Manager s1m;
    public Login userdata;
}
