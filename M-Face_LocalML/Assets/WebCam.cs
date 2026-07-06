using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class WebCam : MonoBehaviour {
    private WebCamTexture camTexture;
    public String seq;
    public RawImage cameraViewImage;

    private void OnEnable() {
        fname = userdata.ID;
        seq = userdata.seq;
        for (int i = 0; i < 4; i++) {
            Guides[i].SetActive(false);
        }
        ac = GetComponent<AudioController>();
        FM = GameObject.FindObjectOfType<FlowManager>();
        sm1 = GameObject.FindObjectOfType<Stage1Manager>();
        cameraViewImage = GetComponent<RawImage>();
        End.gameObject.SetActive(false);
        cameraViewImage.rectTransform.localPosition = new Vector2(700.25f, 0f);
        cameraViewImage.rectTransform.sizeDelta = new Vector2(1400,1752);
        
        //rt.width = 0.5f;
        
        isDone = false;
        incorrectNum = 0;
        if (FM.deviceCount > 0) {
            camTexture = new WebCamTexture(FM.devices[FM.deviceNum].name,1920,1080,30);
            cameraViewImage.texture = camTexture;
            StartCoroutine(CaptureCount());
            StartCoroutine(MFaceLocalEmotionAnalyzer.EnsureInitialized());
        }
       
    }

    private AudioSource asc;
    private AudioController ac;
    public Button End;
    public Button Replay;
    public Button Confirm;
    public Text Timer;
    public float time_count = 3f;
    IEnumerator CaptureCount() {
        Confirm.interactable = true;
        Replay.gameObject.SetActive(false);
        Confirm.gameObject.SetActive(false);
        camTexture.Play();
        ac.playNumber(0);
            
        Rect rt = cameraViewImage.uvRect;
        rt.width = ((float)camTexture.width/camTexture.height)<1.7f ? -0.48f : -0.36f;
        Debug.Log(""+(camTexture.width)+(camTexture.height));
        rt.x = 0.5f-(rt.width/2);
        rt.y = 0.1f;
        rt.height = 0.8f;
        cameraViewImage.uvRect = rt;
        ic.change("P9");
        float t = Time.fixedTime;
        uint z = camTexture.updateCount;
        while (camTexture.updateCount==z||ac.asc.isPlaying) {
            yield return null;
            if (Time.fixedTime - t > 6f)
                break;
        }
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

    }

    public AudioClip shutter;

    private FlowManager FM;
    private bool isDone;
    public void ClickReplay() {
        StartCoroutine(CaptureCount());
    }

    public int incorrectNum;
    public Sprite incorrectInstruction;
    public Sprite incorrectEndInstruction;
    public GameObject[] Guides;
    IEnumerator IncorrectCaptureCount() {
        incorrectNum++;
        if (incorrectNum > 0) 
            isDone = true;
            
        Confirm.interactable = true;
        if(sm1.FaceModel!=null)
            Destroy(sm1.FaceModel);
        for (int i = 0; i < 4; i++) {
            Guides[i].SetActive(false);
        }
        Guides[sm1.correctType].SetActive(true);
        Replay.gameObject.SetActive(false);
        Confirm.gameObject.SetActive(false);
        sm1.isCorrectThisProgress = false;
        ac.playNumber(5);
        ic.img.sprite = incorrectInstruction;
        ic.img.SetNativeSize();
        camTexture.Play();
        
        
        
        float t = Time.fixedTime;
        uint z = camTexture.updateCount;
        while (camTexture.updateCount==z||ac.asc.isPlaying) {
            yield return null;
            if (Time.fixedTime - t > 7f)
                break;
        }
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


        Replay.gameObject.SetActive(true);
        Confirm.gameObject.SetActive(true);
        Timer.gameObject.SetActive(false);
        End.gameObject.SetActive(false);
    }

    IEnumerator Confirmation() {
        yield return StartCoroutine(confirmEmotion());
        if ((!sm1.isCorrectThisProgress)&&(!isDone)) {
            StartCoroutine(IncorrectCaptureCount());
        } 
        else {
            if (!sm1.isCorrectThisProgress && isDone) {
            
                ac.playNumber(6);
                ic.img.sprite = incorrectEndInstruction;
                ic.img.SetNativeSize();
            }
            else {
                ac.playNumber(sm1.correctType + 1);
                
                int correctType = sm1.correctType;
                ic.change("P15" + (correctType == 0 ? "Happy" :
                    correctType == 1 ? "Neutral" :
                    correctType == 2 ? "Sad" : "Anger"));
            }

            if (emotion < 0)
                incorrectNum += 1;
            int tempCnt = incorrectNum == 2 ? 0 : incorrectNum == 1 ? 2 : 0;
            sm1.faceCount += tempCnt;
            //End.gameObject.SetActive(true);
            Replay.gameObject.SetActive(false);
            Confirm.gameObject.SetActive(false);
            cameraViewImage.rectTransform.localPosition = new Vector2(0f, 0f);
            cameraViewImage.rectTransform.sizeDelta = new Vector2(1000, 1000);
            Rect rt = cameraViewImage.uvRect;
            rt.y = 0.1f;
            rt.height = 0.8f;
            rt.width = ((float)camTexture.width/camTexture.height)<1.7f ? -0.6f : -0.45f;
            rt.x = 0.5f-rt.width/2;
            cameraViewImage.uvRect = rt;
            /*
            int correctType = sm1.correctType;
            ic.change("P15" + (correctType == 0 ? "Happy" :
                correctType == 1 ? "Neutral" :
                correctType == 2 ? "Sad" : "Anger"));*/
            if(sm1.FaceModel!=null)
                Destroy(sm1.FaceModel);
            while (ac.asc.isPlaying) {
                yield return null;
            }
            End.gameObject.SetActive(true);
        }
    }
    public void ClickConfirm() {
        //
        if (ac.voiceOn)
            return;
        for (int i = 0; i < 4; i++) {
            Guides[i].SetActive(false);
        }
        StartCoroutine(Confirmation());
    }

    public void ClickEnd() {
        for (int i = 0; i < 4; i++) {
            Guides[i].SetActive(false);
        }
        if (ac.voiceOn)
            return;
        if(sm1.FaceModel!=null)
            Destroy(sm1.FaceModel);
        if(sm1.progressCount<12)    
            sm1.StartTrial();
        else {
            Stage2.SetActive(true);
            userdata.logUpload("{Stage:1;Point:("+sm1.point+"/"+sm1.pointMax+");Face:"+sm1.faceCount+"}");
        }
    }
    public string fname;
    public Login userdata;
    public int emotion;

    public float THR = 0.15f;
    private Emotion em;

    IEnumerator confirmEmotion() {
        Confirm.interactable = false;
        Replay.gameObject.SetActive(false);
        emotion = -1;

        Texture2D capture = null;
        yield return MFaceLocalEmotionAnalyzer.CaptureFrame(cameraViewImage, t => capture = t);

        int localEmotion = -1;
        Emotion localEmotionStruct = null;
        yield return MFaceLocalEmotionAnalyzer.AnalyzeTexture(capture, sm1.correctType, THR, (e, emotionObj, raw) => {
            localEmotion = e;
            localEmotionStruct = emotionObj;
            MFaceAndroidLog.Info("Stage1: " + raw);
        });

        if (capture != null)
            Destroy(capture);

        emotion = localEmotion;
        if (localEmotionStruct != null)
            em = localEmotionStruct;

        sm1.isCorrectThisProgress = emotion == sm1.correctType;
        yield return null;
    }
    public GameObject Stage2;
    public Stage1Manager sm1;
    public InstructionChange ic;

}
