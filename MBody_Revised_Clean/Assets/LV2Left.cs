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
    Coroutine maskRoutine;

    void Awake() {
        EnsureRefs();
    }

    void EnsureRefs()
    {
        if (vp == null)
            vp = GetComponent<VideoPlayer>();
        if (fm == null)
            fm = FindFirstObjectByType<FlowManager>();
        if (dm == null)
            dm = FindFirstObjectByType<DanceManager>();
        if (playButton == null)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            for (var i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.name == "Button")
                {
                    playButton = buttons[i];
                    break;
                }
            }
        }
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
        EnsureRefs();
        MBodyDiagLog.Step("LV2", $"PlaySource enter dance={dm?.danceNum ?? 0} dm.level={dm?.level ?? 0} fm.level={fm?.level ?? 0}");
        if (dm == null || dm.danceNum <= 0)
        {
            MBodyDiagLog.Error("LV2", $"Cannot play: danceNum={dm?.danceNum ?? 0}");
            return;
        }

        int level = PracticeLevel();
        int clipIndex = (dm.danceNum - 1) * 3 + level - 1;
        if (clips == null || clipIndex < 0 || clipIndex >= clips.Length)
        {
            MBodyDiagLog.Error("LV2", $"Invalid clip dance={dm.danceNum} level={level} idx={clipIndex} clips={(clips != null ? clips.Length : 0)}");
            return;
        }

        vp.clip = clips[clipIndex];
        MBodyDiagLog.Step("LV2", $"PlaySource dance={dm.danceNum} level={level} clip={vp.clip.name}");
        RestartVideo();
        isPlaying = true;
        endButtons.gameObject.SetActive(false);
        playButton.gameObject.SetActive(false);
        lastPlayTime = Time.fixedTime;
    }

    int PracticeLevel()
    {
        if (dm != null && dm.level > 0)
            return dm.level;
        if (fm != null && fm.level > 0)
            return fm.level;
        MBodyDiagLog.Warn("LV2", "Practice level unset; defaulting to 2");
        return 2;
    }
    private void OnEnable() {
        EnsureRefs();
        BodyResourceLifecycle.ReleaseBodyCapture();
        DisableVideoScreenRaycasts();
        WirePlayButton();
        if (fm != null)
        {
            fm.setBackground(5);
            fm.setBlackScreen(false);
        }
        if (dm != null && dm.danceNum <= 0)
            MBodyDiagLog.Warn("LV2", "danceNum unset on LV2 entry");
        ChangeIcons(false);
        isPlaying = false;
        isPower = true;
        if (endButtons != null)
            endButtons.SetActive(false);
        if (playButton != null)
            playButton.gameObject.SetActive(true);
        for (int i = 0; i < 4; i++) {
            Masks[i].sprite = maskSprite;
            Masks[i].color = new Color(1f, 1f, 1f, 0f);
        }
        MBodyDiagLog.Step("LV2", $"OnEnable dance={dm?.danceNum ?? 0} level={PracticeLevel()} playBtn={(playButton != null)}");
    }

    void DisableVideoScreenRaycasts()
    {
        var raws = GetComponentsInChildren<RawImage>(true);
        for (var i = 0; i < raws.Length; i++)
        {
            if (raws[i] != null && raws[i].gameObject.name == "VideoScreen")
                raws[i].raycastTarget = false;
        }
    }

    void WirePlayButton()
    {
        if (playButton == null)
            return;

        playButton.onClick.RemoveListener(HandlePlayPressed);
        playButton.onClick.AddListener(HandlePlayPressed);
    }

    void HandlePlayPressed()
    {
        MBodyDiagLog.Step("LV2", "Play button clicked");
        PlaySource();
        StartInteraction();
    }

    void OnDisable()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(HandlePlayPressed);
        if (maskRoutine != null)
        {
            StopCoroutine(maskRoutine);
            maskRoutine = null;
        }
    }

    public void ChangeIcons(bool isSoft) {
        isPower = !isSoft;
        if (dm == null)
            return;
        int dn = dm.danceNum;
        int spd = PracticeLevel();

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
        if (maskRoutine != null)
            StopCoroutine(maskRoutine);
        maskRoutine = StartCoroutine(MaskInteraction());
    }

    public Text[] numbers;
    IEnumerator MaskInteraction() {
        for (int i = 0; i < 4; i++) {
            Masks[i].sprite = maskSprite;
            Masks[i].color = new Color(1f, 1f, 1f, 0f);
            numbers[i].text = "";
        }
        int level = PracticeLevel();
        float term = level == 1 ? 120f / 90f : level == 3 ? 120f / 110f : 120f / 100f;
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
        if (ScenePageVideoDriver.Instance != null)
            ScenePageVideoDriver.Instance.PlayPlayer(vp);
        else if (vp.GetComponent<UiVideoPlayer>() is UiVideoPlayer ui)
            ui.RestartPlayback();
        else
            vp.Play();
    }

}
