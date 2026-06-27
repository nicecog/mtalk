using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class BodyGameScene : MonoBehaviour {
    private FlowManager fm;

    private BodyManager bm;

    private Coroutine startSequenceRoutine;
    private readonly bool[] hitCooldown = new bool[4];

    // Start is called before the first frame update
    private void Awake() {
        bm = FindFirstObjectByType<BodyManager>();
        fm = FindFirstObjectByType<FlowManager>();
        auso = GetComponent<AudioSource>();
        acs = new AudioClip[4];
        BindHandMarkers();
    }

    void BindHandMarkers() {
        if (fm == null)
            return;

        LH = fm.LeftHand;
        RH = fm.RightHand;
        if (LH != null)
            pLeft = LH.rectTransform.anchoredPosition;
        if (RH != null)
            pRight = RH.rectTransform.anchoredPosition;
    }

    public VideoCaptureCam vcc;
    public BodyGameTraining bt;

    public SoundSelect SoundYellow;
    public SoundSelect SoundBlue;
    private AudioClip[] acs;

    public bool isPractice = false;
    private Image LH, RH;
    private AudioSource auso;
    public GameObject blue, yellow;
    
    private IEnumerator PlaySound(int _count) {
        if (targets == null || _count >= targets.Length || targets[_count] == null)
            yield break;

        var v = targets[_count];
        while (true) {
            if (LH == null || RH == null || acs[_count] == null) {
                yield return null;
                continue;
            }

            Vector3 pv = v.rectTransform.anchoredPosition;
            float rightDist = Vector3.Distance(pv, RH.rectTransform.anchoredPosition);
            float leftDist = Vector3.Distance(pv, LH.rectTransform.anchoredPosition);
            bool hit = false;
            if (_count < 2) {
                if ((rightDist < thr * 0.9f && veloRight >= thr_vel) || (leftDist < thr * 0.9f && veloLeft >= thr_vel))
                    hit = true;
            } else if (rightDist < thr || leftDist < thr) {
                hit = true;
            }

            if (hit)
                RegisterHit(_count);

            yield return null;
        }
    }

    IEnumerator fast_alert() {
        if (Alert == null)
            yield break;

        PlayingClock.Clear();
        alert_times++;
        Alert.color = Color.white;
        float startTime = Time.fixedTime;
        Alert.enabled = true;
        while (true) {
            float nowTime = Time.fixedTime;
            PlayingClock.Clear();
            if (startTime + 5.5f < nowTime)
                break;
            if (startTime + 5f < nowTime) {
                Alert.color = new Color(1f,1f,1f,1f - (nowTime-startTime-5f)*2 );
            }
            yield return null;
        }

        Alert.enabled = false;

    }

    public GameObject InputImage;
    public int alert_times = 0;
    public Text txt;
    public float thr_vel = 100f;
    public float thr = 300f;

    public Image[] targets;
    public Image Alert;

    private List<float> PlayingClock;
    // Update is called once per frame
    void Update() {
        if (LH == null || RH == null)
            return;

        veloLeft = Vector3.Distance(LH.rectTransform.anchoredPosition, pLeft);
        veloRight = Vector3.Distance(RH.rectTransform.anchoredPosition, pRight);
        pLeft = LH.rectTransform.anchoredPosition;
        pRight = RH.rectTransform.anchoredPosition;
        int t = (count - 1) / 16;
        if (!isPractice && Effects != null && Effects.Length >= 4) {
            if (t == 2 || t == 3 || t == 5 || t == 8 || t == 9) {
                Effects[0].enabled = false;
                Effects[1].enabled = false;
                Effects[2].enabled = true;
                Effects[3].enabled = true;
            }
            else {
                Effects[0].enabled = true;
                Effects[1].enabled = true;
                Effects[2].enabled = false;
                Effects[3].enabled = false;
            }
        }
    }

    private Vector3 pLeft, pRight;
    private float veloLeft, veloRight;

    public void startGame() {
        if (!isPractice)
            BodyResourceLifecycle.SetPoseProcessing(false);

        if (startSequenceRoutine != null)
            StopCoroutine(startSequenceRoutine);
        startSequenceRoutine = StartCoroutine(StartSequence());
    }

    private void OnEnable() {
        MBodyDiagLog.Step("BodyGame", $"OnEnable practice={isPractice} page={name}");
        result = 0;
        section_result = 0;
        Array.Clear(hitCooldown, 0, hitCooldown.Length);
        ResolveReferences();
        DisableBlockingRaycasts();
        BindHandMarkers();
        if (!isPractice && Alert != null)
            Alert.enabled = false;
        PlayingClock = new List<float>();
        if (!isPractice && nextButton != null)
            nextButton.SetActive(false);
        if (!isPractice)
            BodyResourceLifecycle.SetPoseProcessing(false);
        ApplyInstrumentSetup();

        for (int i = 0; i < 4; i++)
            StartCoroutine(PlaySound(i));

        StartCoroutine(DeferredStartup());
    }

    IEnumerator DeferredStartup() {
        yield return null;
        if (!isActiveAndEnabled)
            yield break;

        MBodyDiagLog.Step("BodyGame", $"DeferredStartup practice={isPractice}");
        BodyResourceLifecycle.EnsureBodyCapture();
        yield return BodyResourceLifecycle.EnsureBodyCaptureWhenReady();

        if (!isActiveAndEnabled)
            yield break;

#if UNITY_ANDROID && !UNITY_EDITOR
        yield return null;
        yield return null;
        yield return new WaitForEndOfFrame();
        if (bm != null && bm.playTimes > 0) {
            for (int i = 0; i < 6; i++)
                yield return null;
            yield return new WaitForEndOfFrame();
        }
#endif

        EnableInputImage();

        if (isPractice) {
            MBodyDiagLog.Step("BodyGame", "DeferredStartup enabling pose after webcam settle");
            BodyResourceLifecycle.SetPoseProcessing(true);
        }

        BindHandMarkers();
        if (fm != null)
            fm.BringHandMarkersToFront();
    }

    void ResolveReferences() {
        if (bm == null)
            bm = FindFirstObjectByType<BodyManager>();
        if (fm == null)
            fm = FindFirstObjectByType<FlowManager>();
        if (dm == null)
            dm = FindFirstObjectByType<DanceManager>();
        if (vcc == null)
            vcc = FindFirstObjectByType<VideoCaptureCam>();
        if (bm != null) {
            bm.ResolvePackRefs();
            if (SoundYellow == null)
                SoundYellow = bm.yellowSelect;
            if (SoundBlue == null)
                SoundBlue = bm.blueSelect;
        }
        if (InputImage == null && fm != null && fm.WebCamObject != null)
            InputImage = fm.WebCamObject.gameObject;
    }

    void EnableInputImage() {
        if (InputImage == null)
            return;

        InputImage.SetActive(true);
        var raw = InputImage.GetComponent<RawImage>();
        if (raw != null)
            raw.enabled = true;
    }

    void DisableBlockingRaycasts() {
        var video = transform.Find("VideoImage");
        if (video != null) {
            var graphic = video.GetComponent<Graphic>();
            if (graphic != null)
                graphic.raycastTarget = false;
        }
    }

    void ApplyInstrumentSetup() {
        if (bm != null)
            bm.CacheInstrumentPicks();

        if (SoundYellow == null || SoundBlue == null) {
            MBodyDiagLog.Warn("BodyGame", "SoundSelect refs missing on BodyGameScene");
            return;
        }

        if (SoundYellow.slots == null || SoundYellow.slots.Length < 2
            || SoundBlue.slots == null || SoundBlue.slots.Length < 2) {
            MBodyDiagLog.Warn("BodyGame", "SoundSelect slots missing on BodyGameScene");
            return;
        }

        var slotSets = new[] {
            SoundYellow.slots[0], SoundYellow.slots[1],
            SoundBlue.slots[0], SoundBlue.slots[1]
        };
        var selects = new[] { SoundYellow, SoundYellow, SoundBlue, SoundBlue };

        for (int i = 0; i < 4; i++) {
            var slot = slotSets[i];
            var select = selects[i];
            if (select == null || targets == null || i >= targets.Length || targets[i] == null)
                continue;

            int idx = bm != null ? bm.GetInstrumentIndex(select, i % 2) : slot != null ? slot.indexData : 0;
            if (idx <= 0) {
                MBodyDiagLog.Warn("BodyGame", $"Target {i} has no instrument (indexData=0)");
                continue;
            }

            if (idx <= select.audio_sources.Length)
                acs[i] = select.audio_sources[idx - 1];

            var sprite = ResolveSlotSprite(slot, select, idx);
            if (sprite != null) {
                targets[i].sprite = sprite;
                targets[i].enabled = true;
                targets[i].color = Color.white;
                targets[i].preserveAspect = true;
            }

            EnsureTapTarget(i);
        }

        MBodyDiagLog.Step("BodyGame", $"Instruments ready practice={isPractice} picks=[{bm?.yellowPicks[0]},{bm?.yellowPicks[1]},{bm?.bluePicks[0]},{bm?.bluePicks[1]}]");
    }

    static Sprite ResolveSlotSprite(DragSlot slot, SoundSelect select, int indexData) {
        if (slot.backIcons != null) {
            var drop = slot.backIcons.GetComponent<DropIcon>();
            if (drop != null && drop.gm != null) {
                var img = drop.gm.GetComponent<Image>();
                if (img != null && img.sprite != null)
                    return img.sprite;
            }
        }

        if (select.Icons == null || indexData <= 0 || indexData > select.Icons.Length)
            return null;

        var icon = select.Icons[indexData - 1];
        if (icon == null || icon.transform.childCount == 0)
            return null;

        var fallback = icon.transform.GetChild(0).GetComponent<Image>();
        return fallback != null ? fallback.sprite : null;
    }

    void EnsureTapTarget(int index) {
        var tap = targets[index].GetComponent<BodyGameTargetTap>();
        if (tap == null)
            tap = targets[index].gameObject.AddComponent<BodyGameTargetTap>();
        tap.scene = this;
        tap.index = index;
        targets[index].raycastTarget = true;
    }

    public void HitTarget(int index) {
        MBodyDiagLog.Step("BodyGame", $"HitTarget index={index} practice={isPractice}");
        RegisterHit(index);
    }

    void RegisterHit(int index) {
        if (index < 0 || index >= acs.Length || acs[index] == null || hitCooldown[index])
            return;

        hitCooldown[index] = true;
        StartCoroutine(ReleaseHitCooldown(index, index < 2
            ? (fm != null && fm.level == 3 ? 1f : 1.2f)
            : 2f));

        if (auso == null)
            auso = GetComponent<AudioSource>();
        if (auso != null)
            auso.PlayOneShot(acs[index]);

        if (isPractice) {
            if (bt != null)
                bt.Played(index);
            return;
        }

        var v = targets != null && index < targets.Length ? targets[index] : null;
        if (v == null)
            return;

        try {
            if (index < 2) {
                if (yellow != null)
                    GameObject.Instantiate(yellow, v.transform);
            } else if (blue != null) {
                GameObject.Instantiate(blue, v.transform);
            }
        } catch (System.Exception ex) {
            MBodyDiagLog.Warn("BodyGame", $"Hit effect failed target={index}: {ex.Message}");
        }

        if (okayTime)
            result++;

        int t = (count - 1) / 16;
        if (index < 2) {
            if (t == 0 || t == 1 || t == 4 || t == 6 || t == 7 || t == 10 || t == 11)
                section_result++;
        } else if (t == 2 || t == 3 || t == 5 || t == 8 || t == 9) {
            section_result++;
        }

        PlayingClock.Add(Time.fixedTime);
        if (PlayingClock.Count > 4)
            PlayingClock.RemoveAt(0);
        if (PlayingClock.Count == 4) {
            bool smaller_than_threshold = true;
            float delay_threshold = fm != null && fm.level == 3 ? 0.5f : 0.6f;
            for (int i = 0; i < 3; i++)
                if (PlayingClock[i + 1] - PlayingClock[i] > delay_threshold)
                    smaller_than_threshold = false;

            if (smaller_than_threshold)
                StartCoroutine(fast_alert());
        }
    }

    IEnumerator ReleaseHitCooldown(int index, float seconds) {
        yield return new WaitForSecondsRealtime(seconds);
        hitCooldown[index] = false;
    }

    float ResolveIntroTime() {
        float[] introTable = fm.level == 1 ? slowIntro : fm.level == 3 ? fastIntro : midIntro;
        if (introTable == null || introTable.Length == 0)
            return 0f;

        int idx = Mathf.Clamp(bm.song_idx, 0, introTable.Length - 1);
        return introTable[idx];
    }

    public float[] slowIntro;
    public float[] midIntro;
    public float[] fastIntro;
    
    public int count;
    public IEnumerator StartSequence() {
        yield return null;
        if (bm == null || fm == null) {
            MBodyDiagLog.Warn("BodyGame", "StartSequence missing BodyManager or FlowManager");
            if (nextButton != null)
                nextButton.SetActive(true);
            yield break;
        }

        if (fm.level <= 0)
            fm.level = 2;

        alert_times = 0;
        AudioSource bmas = bm.GetComponent<AudioSource>();
        if (bmas == null) {
            MBodyDiagLog.Warn("BodyGame", "BodyManager AudioSource missing");
            if (nextButton != null)
                nextButton.SetActive(true);
            yield break;
        }

        int bgmIndex = bm.song_idx * 3 - 1 + fm.level;
        if (bm.bgms == null || bgmIndex < 0 || bgmIndex >= bm.bgms.Length) {
            MBodyDiagLog.Warn("BodyGame", $"Invalid bgm index {bgmIndex} song={bm.song_idx} level={fm.level}");
            if (nextButton != null)
                nextButton.SetActive(true);
            yield break;
        }

        bmas.clip = bm.bgms[bgmIndex];
        bmas.loop = false;
        if (bm.playTimes == 3 && !isPractice && vcc != null) {
            vcc.prefix = "BodyThirdPlay";
            vcc.startRecord();
        }

        float term = fm.level == 1 ? 60 / 90f : fm.level == 3 ? 60 / 110f : 60 / 100f;
        count = 0;
        MBodyDiagLog.Step("BodyGame", $"StartSequence intro wait song={bm.song_idx} level={fm.level}");
        yield return new WaitForSecondsRealtime(3f);

        // MainGame uses touch (BodyGameTargetTap); keep BlazePose off to avoid
        // GPU buffer races while BGM plays and the user taps instruments.
        if (!isPractice)
            BodyResourceLifecycle.SetPoseProcessing(false);

        if (bmas.clip == null) {
            MBodyDiagLog.Warn("BodyGame", "BGM clip is null");
            if (nextButton != null)
                nextButton.SetActive(true);
            yield break;
        }

        bmas.Play();
        okayTime = false;
        float intro = ResolveIntroTime();
        while (true) {
            if (bmas.isPlaying && nextButton != null)
                nextButton.SetActive(false);

            if (bmas.time >= intro + count * term)
                count++;

            if (bmas.time - intro >= count * term - term / 4f || bmas.time - intro <= (count - 1) * term + term / 4f)
                okayTime = true;
            else
                okayTime = false;

            yield return null;
            if (bmas.time >= bmas.clip.length || count > 192) {
                if (nextButton != null)
                    nextButton.SetActive(true);
                break;
            }

            if (nextButton != null)
                nextButton.SetActive(false);
        }

        yield return new WaitForSecondsRealtime(2f);
        if (nextButton != null)
            nextButton.SetActive(true);

        if (!isPractice) {
            if (bm.playTimes == 3 && vcc != null)
                vcc.stopRecording(true, 0);
            while (bmas.isPlaying)
                yield return null;
            yield return new WaitForSecondsRealtime(2f);
            if (nextButton != null) {
                var button = nextButton.GetComponent<Button>();
                if (button != null)
                    button.onClick.Invoke();
            }
        }
    }

    private bool okayTime = false;
    public float decay = 0.96f;
    public int result = 0;
    public Image[] Effects;
    public GameObject nextButton;
    public int section_result = 0;
    private void OnDisable() {
        StopAllCoroutines();
        startSequenceRoutine = null;
        BodyResourceLifecycle.SetPoseProcessing(false);
        if (fm != null)
            fm.RestoreHandMarkers();

        if (!isPractice) {
            if (txt != null)
                txt.text = MBodyKoreanText.BodyScoreMessage(Math.Min(result + section_result, 50));
            if (dm != null && dm.fileManager != null && bm != null && fm != null)
                dm.fileManager.WriteResult("Body," + bm.playTimes + "," + (result + section_result) + "," + "SongIndex:" + (bm.song_idx + 1) + "|SongSpeed:" + (fm.level) + "|Alert:" + alert_times + "|Rhythm:" + result + "|Section:" + section_result);
        }

        if (bm != null) {
            AudioSource bmas = bm.GetComponent<AudioSource>();
            if (bmas != null)
                bmas.loop = true;
        }
    }

    public DanceManager dm;
}