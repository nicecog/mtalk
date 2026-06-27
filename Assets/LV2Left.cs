using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class LV2Left : MonoBehaviour
{
    // Start is called before the first frame upd3ate
    private float dh = 313f;
    public bool isPower;
    public FlowManager fm;
    void Awake() {
        vp = GetComponent<VideoPlayer>();
        fm = FindFirstObjectByType<FlowManager>();
        dm = FindFirstObjectByType<DanceManager>();
    }

    void Update() {
        if (!vp.isPlaying && isPlaying&&Time.fixedTime>lastPlayTime+3f) {
            isPlaying = false;
            playButton.gameObject.SetActive(true);
            endButtons.SetActive(true);
        }
    }

    private bool isPlaying;
    public GameObject endButtons;
    public Button playButton;
    private float lastPlayTime;

    public void PlaySource() {
        vp.clip = clips[(dm.danceNum - 1) * 3 + fm.level - 1];
        RestartVideo();
        isPlaying = true;
        endButtons.gameObject.SetActive(false);
        playButton.gameObject.SetActive(false);
        lastPlayTime = Time.fixedTime;
    }
    private void OnEnable() {
        BodyResourceLifecycle.ReleaseBodyCapture();
        fm.setBackground(5);
        fm.setBlackScreen(false);
        ChangeIcons(false);
        isPlaying = false;
        isPower = true;
        endButtons.SetActive(false);
        playButton.gameObject.SetActive(true);
        for (int i = 0; i < 4; i++) {
            Masks[i].sprite = maskSprite;
            Masks[i].color = new Color(1f, 1f, 1f, 0f);
        }
    }

    public void ChangeIcons(bool isSoft) {
        isPower = !isSoft;
        int dn = dm.danceNum;
        int spd = fm.level;

        int[] SoftSequences = new int[4];
        int[] PowerSequences = new int[4];

        switch (dn) {
            case 1:
                SoftSequences = new int[4] { 1,1,0,0};
                PowerSequences = new int[4] { 0,1,3,2};
                break;
            case 2:
                SoftSequences = new int[4] { 1,0,2,3};
                PowerSequences = new int[4] { 1,1,3,3};
                break;
            case 3:
                SoftSequences = new int[4] {0,1,3,2 };
                PowerSequences = new int[4] { 1,0,3,2};
                break;
            case 4:
                SoftSequences = new int[4] { 0,1,3,2};
                PowerSequences = new int[4] { 0,1,2,3};
                break;
            case 5:
                SoftSequences = new int[4] { 2,0,0,2};
                PowerSequences = new int[4] {1,1,3,3 };
                break;
            case 6:
                SoftSequences = new int[4] { 2,2,0,0};
                PowerSequences = new int[4] {0,2,0,3 };
                break;
        }
        
        
        for (int i = 0; i < 4; i++) {
            Icons[i].sprite = isSoft ? SoftSprites[SoftSequences[i] + (dn-1) * 4] : PowerSprites[PowerSequences[i] + (dn-1) * 4];
            Icons[i].SetNativeSize();
            Icons[i].rectTransform.sizeDelta =
                Icons[i].rectTransform.sizeDelta * dh / Icons[i].rectTransform.rect.height;
        }
    }

    public DanceManager dm;
    public Image[] Masks;
    public Image[] Icons;

    public Sprite[] SoftSprites;
    public Sprite[] PowerSprites;
    public Sprite SoftOutline;
    public Sprite HardOutline;
    public Sprite maskSprite;
    public VideoPlayer vp;
    public void StartInteraction() {
        StartCoroutine(MaskInteraction());
    }

    public Text[] numbers;
    IEnumerator MaskInteraction() {
        for (int i = 0; i < 4; i++) {
            Masks[i].sprite = maskSprite;
            Masks[i].color = new Color(1f, 1f, 1f, 0f);
            numbers[i].text = "";
        }
        float term = fm.level == 1 ? 120f / 90f : fm.level == 3 ? 120f / 110f : 120f / 100f;
        while (true) {
            yield return null;
            if (vp.time >= term * 4)
                break;
        }
        ChangeIcons(false);
        int count = 0;
        double vptime = vp.time-0.3f;
        for (int i = 0; i < 4; i++) {
            Masks[i].sprite = i == count % 4 ? (isPower?HardOutline:SoftOutline) :maskSprite;
            Masks[i].color = i == count % 4 ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 200f / 255f);
            numbers[i].text = i == count % 4 ? String.Concat(i + 1) : "";
        }
        while (count<16) {

            if (vp.time>=vptime+(count+1)*term) {
                count++;
                for (int i = 0; i < 4; i++) {
                    Masks[i].sprite = i == count % 4 ? (isPower?HardOutline:SoftOutline) :maskSprite;
                    Masks[i].color = i == count % 4 ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 200f / 255f);
                    numbers[i].text = i == count % 4 ? String.Concat(i + 1) : "";
                }
            }

            yield return null;
            
        }

        count = 0;
        ChangeIcons(true);

        vptime = vptime + 16 * term;
        
        for (int i = 0; i < 4; i++) {
            Masks[i].sprite = i == count % 4 ? (isPower?HardOutline:SoftOutline) :maskSprite;
            Masks[i].color = i == count % 4 ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 200f / 255f);
            numbers[i].text = i == count % 4 ? String.Concat(i + 1) : "";
        }
        while (count<16) {

            if (vp.time>=vptime+(count+1)*term) {
                count++;
                for (int i = 0; i < 4; i++) {
                    Masks[i].sprite = i == count % 4 ? (isPower?HardOutline:SoftOutline) :maskSprite;
                    Masks[i].color = i == count % 4 ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 200f / 255f);
                    numbers[i].text = i == count % 4 ? String.Concat(i + 1) : "";
                }
            }

            yield return null;
        }
        ChangeIcons(false);
        count = 0;
        vptime = vptime+ 16 * term;
        for (int i = 0; i < 4; i++) {
            Masks[i].sprite = i == count % 4 ? (isPower?HardOutline:SoftOutline) :maskSprite;
            Masks[i].color = i == count % 4 ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 200f / 255f);
            numbers[i].text = i == count % 4 ? String.Concat(i + 1) : "";
        }
        while (count<16) {

            if (vp.time>=vptime+(count+1)*term) {
                count++;
                for (int i = 0; i < 4; i++) {
                    Masks[i].sprite = i == count % 4 ? (isPower?HardOutline:SoftOutline) :maskSprite;
                    Masks[i].color = i == count % 4 ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 200f / 255f);
                    numbers[i].text = i == count % 4 ? String.Concat(i + 1) : "";
                }
            }

            yield return null;
        }

        count = 0;
        ChangeIcons(true);
        vptime = vptime+ 16 * term;
        
        for (int i = 0; i < 4; i++) {
            Masks[i].sprite = i == count % 4 ? (isPower?HardOutline:SoftOutline) :maskSprite;
            Masks[i].color = i == count % 4 ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 200f / 255f);
            numbers[i].text = i == count % 4 ? String.Concat(i + 1) : "";
                
        }
        while (count<16) {

            
            if (vp.time>=vptime+(count+1)*term) {
                count++;
                for (int i = 0; i < 4; i++) {
                    Masks[i].sprite = i == count % 4 ? (isPower?HardOutline:SoftOutline) :maskSprite;
                    Masks[i].color = i == count % 4 ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 200f / 255f);
                    numbers[i].text = i == count % 4 ? String.Concat(i + 1) : "";
                
                }
            }

            yield return null;
        }
        
        for (int i = 0; i < 4; i++) {
            Masks[i].sprite = maskSprite;
            Masks[i].color = new Color(1f, 1f, 1f, 0f);
            numbers[i].text = "";
        }
    }

    public VideoClip[] clips;

    void RestartVideo()
    {
        var ui = vp.GetComponent<UiVideoPlayer>();
        if (ui != null)
            ui.RestartPlayback();
        else
            vp.Play();
    }

    private void OnDisable() {
        StopCoroutine(MaskInteraction());
    }
}
