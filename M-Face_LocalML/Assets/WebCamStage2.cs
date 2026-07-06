using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WebCamStage2 : MonoBehaviour {
    private WebCamTexture camTexture;
    public Image icon;
    public Sprite alpha;
    public Vector2 original;
    public RawImage cameraViewImage;
    private void OnEnable() {
        seq = userdata.seq;
        ItemGetScene.SetActive(false);
        itemSel.endPop.SetActive(false);
        int x = itemSel.selectedIdx;
        x = x - 1;
        int y = sm2.songIdx;
        int z = y * 2 + x;
        icon.sprite = itemSel.ItemList[z];
        original = icon.rectTransform.sizeDelta;
        icon.SetNativeSize();
        Vector2 v = icon.rectTransform.sizeDelta;
        if (v.x > v.y) {
            float f = original.x/v.x;
            icon.rectTransform.sizeDelta = v * f;
        }
        else {
            float f = original.y / v.y;
            icon.rectTransform.sizeDelta = v * f;
        }
        ac = GetComponent<AudioController>();
        cameraViewImage = GetComponent<RawImage>();
        End.gameObject.SetActive(false);
        if (FM.deviceCount > 0) {
            camTexture = new WebCamTexture(FM.devices[FM.deviceNum].name,1920,1080,30);
            cameraViewImage.texture = camTexture;
            StartCoroutine(MFaceLocalEmotionAnalyzer.EnsureInitialized());
        }
       
    }
    public void startGame() {
        StartCoroutine(CaptureCount());
    }
    public int correctType;
    private string at;
    private AudioController ac;
    public Button End;
    public Button Replay;
    public Button Confirm;
    public Text Timer;
    public float time_count = 3f;
    
    
    IEnumerator CaptureCount() {
        adminText.text = "";
        Confirm.interactable = true;
        //Replay.gameObject.SetActive(true);
        if (itemSel.progressCount <= 20) {
            int count = itemSel.progressCount-1;
            count = (count % 5) + 1;
            int temp = itemSel.progressCount;
            while (temp > 5)
                temp -= 5;
            itemSel.guageText.text = temp.ToString() + "/5"; 
            itemSel.guageBar.fillAmount = (temp)/5f;
        }
        ac.asc.Stop();
        
        CaptureButton.gameObject.SetActive(false);
        Replay.gameObject.SetActive(false);
        Confirm.gameObject.SetActive(false);
        End.gameObject.SetActive(false);
        float t = Time.fixedTime;
        uint z = camTexture.updateCount;
        camTexture.Play();
        
        Rect rt = cameraViewImage.uvRect;
        rt.y = 0.1f;
        rt.height = 0.8f;
        rt.width = ((float)camTexture.width/camTexture.height)<1.7f ? -0.6f : -0.45f;
        rt.x = 0.5f-rt.width/2;
        cameraViewImage.uvRect = rt;
        while (camTexture.updateCount==z) {
            yield return null;
            if (Time.fixedTime - t > 5f)
                break;
        }
        yield return new WaitForSeconds(0.5f);
        
        correctType = UnityEngine.Random.Range(0, 4);
        at="";
        at = at + (correctType == 0 ? "H" : correctType == 1 ? "N" : correctType == 2 ? "S" : "A");
        at = at + (UnityEngine.Random.Range(0, 2) == 0 ? "1-" : "2-"); //: 난이도 증가
        at = at + UnityEngine.Random.Range(1, 12).ToString("00");
        int rand = UnityEngine.Random.Range(0, 3);
        at = at + (rand == 0 ? "a" : rand == 1 ? "b" : "c");
        at = at + ".mp3";
        itemSel.ic2.updateImage(0);
        ac.playClip(at);
        
        yield return new WaitForSeconds(13f);
        CaptureButton.gameObject.SetActive(true);
        captureOn = false;
        while (true) {
            yield return null;
            if (captureOn == true)
                break;
        }
        captureOn = false;
        CaptureButton.gameObject.SetActive(false);
        float s = Time.fixedTime;
        Timer.gameObject.SetActive(true);
        ac.asc.PlayOneShot(shutter);
        while (true) {
            yield return null;
            float now = Time.fixedTime;
            if (now - s > time_count)
                break;
            Timer.text = Math.Ceiling(time_count - (now - s)).ToString();
        }
        camTexture.Pause();
        yield return new WaitForSeconds(1f);
        ac.playNumber(5);
        itemSel.ic2.updateImage(1);
        yield return new WaitForSeconds(2.5f);
        Replay.gameObject.SetActive(true);
        Confirm.gameObject.SetActive(true);
        Timer.gameObject.SetActive(false);
    }

    public AudioClip shutter;

    public Text adminText;
    public Button CaptureButton;
    public bool captureOn;

    public void captureOnButtonClick() {
        if (ac.voiceOn)
            return;
        captureOn = true;
    }
    IEnumerator ReCaptureCount() {
        
        ac.asc.Stop();
        Replay.gameObject.SetActive(false);
        Confirm.gameObject.SetActive(false);
        camTexture.Play();
        itemSel.ic2.updateImage(0);
        CaptureButton.gameObject.SetActive(true);
        captureOn = false;
        while (true) {
            yield return null;
            if (captureOn == true)
                break;
        }
        captureOn = false;
        CaptureButton.gameObject.SetActive(false);
        float s = Time.fixedTime;
        Timer.gameObject.SetActive(true);
        ac.asc.PlayOneShot(shutter);
        while (true) {
            yield return null;
            float now = Time.fixedTime;
            if (now - s > time_count)
                break;
            Timer.text = Math.Ceiling(time_count - (now - s)).ToString();
        }
        
        camTexture.Pause();
        yield return  new WaitForSeconds(1f);
        ac.playNumber(5);
        itemSel.ic2.updateImage(1);
        Replay.gameObject.SetActive(true);
        Confirm.gameObject.SetActive(true);
        Timer.gameObject.SetActive(false);
    }
    private void OnDisable() {
        if (camTexture != null) {
            camTexture.Stop();
            WebCamTexture.Destroy(camTexture);
            camTexture = null;
        }

        icon.sprite = alpha;
        icon.rectTransform.sizeDelta = original;
        //itemSel.progressCount++;
        
        itemSel.guageBar.fillAmount = 0.2f;
        itemSel.guageText.text = "1/5";
        itemSel.updateItem();
        //itemSel.selectButton(itemSel.selectedIdx);
        if(itemSel.gameObject.activeSelf&&itemSel.sum<2)
            itemSel.ac.playNumber(1);
        itemSel.intro.sprite = itemSel.introSprite[1];
        itemSel.intro.SetNativeSize();
        

    }

    public GameObject ItemGetScene;

    public FlowManager FM;

    public void ClickReplay() {
        StartCoroutine(ReCaptureCount());
    }

    public void ClickConfirm() {
        if (ac.voiceOn)
            return;
        StartCoroutine(confirmEmotion());
    }

    public String seq;
    public string fname;
    public Login userdata;

    public float THR = 0.15f;
    private Emotion em;
    public int emotion;

    IEnumerator confirmEmotion() {
        Confirm.interactable = false;
        Replay.gameObject.SetActive(false);
        emotion = -1;

        Texture2D capture = null;
        yield return MFaceLocalEmotionAnalyzer.CaptureFrame(cameraViewImage, t => capture = t);

        int localEmotion = -1;
        Emotion localEmotionStruct = null;
        yield return MFaceLocalEmotionAnalyzer.AnalyzeTexture(capture, correctType, THR, (e, emotionObj, raw) => {
            localEmotion = e;
            localEmotionStruct = emotionObj;
            adminText.text = raw;
            MFaceAndroidLog.Info("Stage2: " + raw);
        });

        if (capture != null)
            Destroy(capture);

        emotion = localEmotion;
        if (localEmotionStruct != null)
            em = localEmotionStruct;

        float isCorrect = 1f;
        if (emotion == correctType) {
            ac.playNumber(correctType + 1);
            itemSel.ic2.updateImage(correctType + 2);
            Replay.gameObject.SetActive(false);
            Confirm.gameObject.SetActive(false);


            while (ac.asc.isPlaying) yield return null;
            End.gameObject.SetActive(true);
        }
        else {
            isCorrect = 0f;
            ac.playNumber(6);
            itemSel.ic2.updateImage(6);
            Replay.gameObject.SetActive(false);
            Confirm.gameObject.SetActive(false);
            
            
            while (ac.asc.isPlaying) yield return null;
            End.gameObject.SetActive(true);
        }

        float max = 1f;
        if (at.Contains("2-")) max *= 1.5f;
        itemSel.pointMax += max;
        itemSel.point += max * isCorrect;
        yield return null;
    }

    public void ClickEnd() {
        if (ac.voiceOn)
            return;
        if (itemSel.progressCount <= 10) {
            if(itemSel.progressCount%5!=0)
                itemSel.endPop.SetActive(true);
            else {
                ClickLastEnd();
            }
        }
        else {
            transform.parent.gameObject.SetActive(false);
            userdata.logUpload("{Stage:2;Point:("+itemSel.point+"/"+itemSel.pointMax+")}");
            nextPage.SetActive(true);
        }
    }
    
    
    public void ClickLastEnd() {
        itemSel.endPop.SetActive(false);
        itemSel.progressCount++;
        if (itemSel.progressCount % 5 == 1) {
            itemSel.updateItem();
            if (itemSel.sum >= 2)
                return;
            int x = itemSel.selectedIdx;
            x = x - 1;
            int y = sm2.songIdx;
            int z = y * 2 + x;
            bool getItem = itemSel.getItem(z);
            if (getItem) {
                ItemGetScene.SetActive(true);
                ItemGetScene.transform.GetChild(5).GetComponent<Image>().sprite = itemSel.ItemList[z];
                ItemGetScene.transform.GetChild(5).GetComponent<Image>().SetNativeSize();
            }
            else
                transform.parent.gameObject.SetActive(false);
        }
        else
            startGame();
    }

    public GameObject nextPage;
    public Stage2Manager sm2;
    public ItemSelect itemSel;
    

}

public class Emotion {
    public float Neutral;
    public float Happy;
    public float Sad;
    public float Surprise;
    public float Fear;
    public float Disgust;
    public float Anger;
}