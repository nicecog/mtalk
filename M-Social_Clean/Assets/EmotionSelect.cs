using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class EmotionSelect : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable() {
        selectedGlow.gameObject.SetActive(false);
        con.playNumber(0);
        NextButton.gameObject.SetActive(false);
        selectedIndex = 0;
        int temp = scp.SceneEmotions[scp.SceneIndex];
        topMent.sprite = TopMentSprites[0];
        topMent.SetNativeSize();
        emotionPerButton = new int[3];
        emotionPerButton[0] = temp / 100 - 1;
        temp = temp % 100;
        emotionPerButton[1] = temp / 10 - 1;
        temp = temp % 10;
        emotionPerButton[2] = temp - 1;
        for(int i=0;i<3;i++)
            SelectButtons[i].GetComponent<Image>().sprite = EmotionIcons[emotionPerButton[i]];
        pos.gameObject.SetActive(false);
        neg.gameObject.SetActive(false);
    }

    public ScenePlayer scp;


    public void selectEmotionButton(int idx) {
        
        if (selectedIndex == idx) {
            selectedIndex = 0;
            selectedGlow.gameObject.SetActive(false);
            NextButton.gameObject.SetActive(false);
            BGM.asc.Stop();
        }
        else {
            con.asc.Stop();
            selectedIndex = idx;
            selectedGlow.gameObject.SetActive(true);
            selectedGlow.rectTransform.anchoredPosition = 
                new Vector2(SelectButtons[idx - 1].anchoredPosition.x,selectedGlow.rectTransform.anchoredPosition.y);
            emotion = emotionPerButton[idx - 1];
            topMent.sprite = TopMentSprites[emotion + 1];
            topMent.SetNativeSize();
            con.playNumber(emotion+1);
            BGM.asc.Stop();
            BGM.playNumber((emotion)*3 + Random.Range(0,3));
            NextButton.gameObject.SetActive(true);
        }
    }


    public Login userdata;
    public int emotion;
    public void ClickNextButton() {
        if (con.voiceOn)
            return;
        int emo = emotionPerButton[selectedIndex - 1];
        String emoStr = emo == 0 ? "Anger" :
            emo == 1 ? "Sad" :
            emo == 2 ? "Fear" :
            emo == 3 ? "Anxiety" :
            emo == 4 ? "Surprised" :
            emo == 5 ? "Neutral" :
            emo == 6 ? "Comfortable" : "others";
        userdata.logUpload("{Stage:FeelGood;Progress:("+(scp.sceneselect.s1m.progress+1) +"/4);Scene:"+(scp.SceneIndex+1)+";"+
                           "SelectedEmotion:"+emoStr+";isCorrect:"+(emo>4?"Correct":"Incorrect") +"}");
        
        gameObject.SetActive(false);
        if (emotion > 4) {
            pos.mainEmotion = emotionPerButton[0];
            pos.selectedEmotion = emotion;
            pos.gameObject.SetActive(true);
        }
        else {
            neg.mainEmotion = emotion;
            neg.gameObject.SetActive(true);
        }
    }

    public Stage1Positive pos;
    public Stage1Negative neg;
    public int[] emotionPerButton;
    public Image topMent;
    public Sprite[] TopMentSprites;
    public Sprite[] EmotionIcons;
    public int selectedIndex;

    public RectTransform[] SelectButtons;
    public Image selectedGlow;
    public Button NextButton;
    public AudioController con;
    public AudioController BGM;
}
