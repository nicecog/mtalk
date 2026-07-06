using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

public class Stage1Manager : MonoBehaviour {
    public float point;
    public float pointMax;
    public Image guageBar;
    public Login userdata;
    public Sprite[] happyNeutralSadAnger;

    public Image selectGlow;

    public Button confirm;

    public Image[] cards;
    public List<int> trialTypes;
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
    
    private readonly float[] progress = {
        0.062f, 0.15f, 0.235f, 0.325f, 0.41f, 0.5f, 0.585f, 0.675f, 0.765f, 0.85f, 0.938f, 1f
    };

    private void Awake() {
        trialTypes = new List<int>();
    }

    public int progressCount;
    // Start is called before the first frame update
    void Start()
    {
        
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public String faceCount;
    public AudioController con;
    private void OnEnable() {
        con = GetComponent<AudioController>();
        Stage2.SetActive(false);
        trialTypes.Clear();
        point = 0f;
        pointMax = 0f;
        faceCorrect = 0;
        faceCount = "";
        for (int i = 0; i < 12; i++) {
            trialTypes.Add(i/3);
        }
        bool isThree = false;
        do {
            trialTypes = ShuffleList(trialTypes);
            for (int i = 0; i < 12 - 2; i++) {
                isThree = (trialTypes[i] == trialTypes[i + 1]) && (trialTypes[i + 1] == trialTypes[i + 2]);
                if (isThree)
                    break;
            }
        } while (trialTypes[0] > 1 || (isThree));

        progressCount = 0;
        selectedCard = 0;
        confirm.gameObject.SetActive(false);
        selectGlow.gameObject.SetActive(false);
        StartTrial();
    }
    public bool isCorrectThisProgress;

    public Text guageText;
    private List<int> answerList;
    public int faceCorrect;
    public void getPoint(int isCorrect) {
        float r = 1f;
        if (ac.Contains("2-")) r *= 1.5f;
        pointMax += r; 
        point += r*isCorrect;
    }
    public int correctType;
    public void StartTrial() {
        ic.sub.enabled = false;
        isCorrectThisProgress = true;
        ic.change("Stage1Select");
        trialCorrect = true;
        CamStage.SetActive(false);
        CorrectConfirm.gameObject.SetActive(false);
        progressCount++;
        if (progressCount <= 12) {
            guageText.text = progressCount.ToString() + "/12"; 
            guageBar.fillAmount = progress[progressCount - 1];
        }

        correctType = trialTypes[progressCount - 1];
        answerList = new List<int>();
        for (int i = 0; i < 4; i++) {
            answerList.Add(i);
        }

        answerList.Remove(correctType);
        answerList = ShuffleList(answerList);
        answerList.RemoveAt(2);
        answerList.Add(correctType);
        answerList = ShuffleList(answerList);


        for(int i=0;i<3;i++) {
            cards[i].gameObject.SetActive(true);
            cards[i].sprite = happyNeutralSadAnger[answerList[i]];
            cards[i].GetComponent<Button>().interactable = true;
            cards[i].color = Color.white;
            Incorrect[i].gameObject.SetActive(false);
        }
        ac="";
        ac = ac + (correctType == 0 ? "H" : correctType == 1 ? "N" : correctType == 2 ? "S" : "A");
        ac = ac + UnityEngine.Random.Range(1, 3).ToString("0")+"-"; //"2-" : 난이도 증가
        ac = ac + UnityEngine.Random.Range(1, 12).ToString("00");
        int rand = UnityEngine.Random.Range(0, 3);
        ac = ac + (rand == 0 ? "a" : rand == 1 ? "b" : "c");
        ac = ac + ".mp3";
        con.playClip(ac);

    }

    public string ac;
    

    public int selectedCard;
    public void ClickCard(int select) {
        if (con.voiceOn || icCon.voiceOn) {
            if (con.voiceOn) {
                ic.sub.enabled = true;
                ic.sub.sprite = ic.subSprites[4];
                ic.sub.SetNativeSize();
                
            }

            return;
        }

        if (selectedCard == 0 || selectedCard != select) {
            selectedCard = select;
            selectGlow.gameObject.SetActive(true);
            selectGlow.rectTransform.position = cards[selectedCard - 1].rectTransform.position;
            selectGlow.SetNativeSize();
        } else if (selectedCard == select) {
            selectedCard = 0;
            selectGlow.gameObject.SetActive(false);
        }

        confirm.gameObject.SetActive(selectedCard > 0);
    }

    public bool trialCorrect = true; 
    public Sprite correctGlowSprite;
    private GameObject correctGlow;
    public void CheckAnswer() {
        ic.sub.sprite = ic.alpha;
        if (answerList[selectedCard - 1] == correctType) {
            if (isCorrectThisProgress) {
                getPoint(1);
            }
            

            con.asc.Stop();
            correctGlow = GameObject.Instantiate(selectGlow.gameObject,selectGlow.transform.parent);
            Image cimg = correctGlow.GetComponent<Image>();
            cimg.sprite = correctGlowSprite;
            cimg.color=Color.white;
            Destroy(correctGlow.GetComponent<Animator>());
            selectGlow.gameObject.SetActive(false);
            for (int i = 0; i < 3; i++) {
                if (i != selectedCard - 1)
                    cards[i].rectTransform.localScale = Vector3.one * 0.8f;
                cards[i].GetComponent<Button>().interactable = false;
                Incorrect[i].gameObject.SetActive(false);
                cards[i].color = Color.white;
                StartCoroutine(delayedPop());
            }
            ic.change("P8"+(correctType==0 ? "Happy" : correctType==1 ? "Neutral" : correctType==2 ? "Sad" : "Anger"));
            con.playNumber(correctType+1);
            
        }
        else {
            con.asc.Stop();
            if (isCorrectThisProgress) {
                getPoint(0);
            }
            isCorrectThisProgress = false;
            Incorrect[selectedCard-1].gameObject.SetActive(true);
            cards[selectedCard - 1].GetComponent<Button>().interactable = false;
            cards[selectedCard-1].color = new Color(0.75f,0.75f,0.75f,1f);
            int z = answerList[selectedCard - 1];
            selectedCard = 0;
            selectGlow.gameObject.SetActive(false);
            trialCorrect = false;
            ic.change("Incorrect"+(z==0 ? "Happy" : z==1 ? "Neutral" : z==2 ? "Sad" : "Anger")+
                      "Sub"+ (correctType==0 ? "Happy" : correctType==1 ? "Neutral" : correctType==2 ? "Sad" : "Anger"));
        } 
        confirm.gameObject.SetActive(false);
        
    }

    IEnumerator delayedPop() {
        yield return new WaitForSeconds(3.5f);
        CorrectConfirm.gameObject.SetActive(true);
    }
    
    public Button CorrectConfirm;
    public GameObject FaceModel;
    public void AfterCorrectConfirm() {
        if (con.voiceOn||icCon.voiceOn)
            return;
        Destroy(correctGlow);
        CorrectConfirm.gameObject.SetActive(false);
        
        for (int i = 0; i < 3; i++) {
            if (i == selectedCard - 1) {
                FaceModel = GameObject.Instantiate(cards[i].gameObject, cards[i].transform.parent);
                FaceModel.GetComponent<Image>().rectTransform.position = cards[0].rectTransform.position;
                Destroy(FaceModel.GetComponent<Button>());
            }

            cards[i].rectTransform.localScale = Vector3.one;
            cards[i].gameObject.SetActive(false);
        }
        if(progressCount<=12)
            CamStage.SetActive(true);
    }

    
    
    public GameObject Stage2;
    public GameObject CamStage;
    public Image[] Incorrect;
    public InstructionChange ic;
    public AudioController icCon;
}
