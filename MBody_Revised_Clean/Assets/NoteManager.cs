using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class NoteManager : MonoBehaviour {
    public RectTransform Slice1, Slice2, Slice3, Slice4;

    public float tr1, tr2, tr3;
    public Transform noteboxHead;
    public Sprite[] noteTypes;
    public FlowManager fm;
    public DanceStarManager dsm;
    public DanceManager dm;
    public BodyManager bm;
    public VideoCaptureCam vcc;
    public GameObject nextButton;
    public Text txt;
    public AudioClip[] bgms;

    Coroutine noteRoutine;
    Coroutine webcamRoutine;
    AudioSource auso;

    // Dance BGM base index per danceNum (1..6) — same as original client.
    static readonly int[] DanceBgmBase = { 1, 4, 6, 3, 0, 2 };

    void Awake() {
        ResolveRefs();
        _Play = false;
        auso = GetComponent<AudioSource>();
    }

    void ResolveRefs() {
        if (fm == null)
            fm = FindFirstObjectByType<FlowManager>(FindObjectsInactive.Include);
        if (dm == null)
            dm = FindFirstObjectByType<DanceManager>(FindObjectsInactive.Include);
        if (bm == null)
            bm = FindFirstObjectByType<BodyManager>(FindObjectsInactive.Include);
        if (dsm == null)
            dsm = FindFirstObjectByType<DanceStarManager>(FindObjectsInactive.Include);
        if (vcc == null)
            vcc = FindFirstObjectByType<VideoCaptureCam>(FindObjectsInactive.Include);
        if (auso == null)
            auso = GetComponent<AudioSource>();
    }

    public void startNote() {
        ResolveRefs();
        if (noteRoutine != null)
            StopCoroutine(noteRoutine);
        _Play = true;
        // Same note size as level 1; stages 2/3 only run slightly faster (music pitch + note timing).
        float baseTerm = 480f / 100f;
        float speed = GetStageSpeed();
        noteRoutine = StartCoroutine(createNotes(baseTerm / speed, speed));
    }

    /// <summary>Level 1 = 1.0x, level 2 ≈ 1.1x, level 3 ≈ 1.2x.</summary>
    float GetStageSpeed() {
        int level = dsm != null ? dsm.GameLevel : 1;
        if (level >= 3)
            return 1.2f;
        if (level >= 2)
            return 1.1f;
        return 1f;
    }

    public bool _Play { get; set; }

    void OnEnable() {
        ResolveRefs();
        // Hide clear/next immediately so a stale active button cannot show before play starts.
        if (nextButton != null)
            nextButton.SetActive(false);

        if (dsm != null)
            dsm.RefreshNoteTypes();

        EnsureNoteUiVisible();

        if (webcamRoutine != null)
            StopCoroutine(webcamRoutine);
        webcamRoutine = StartCoroutine(PrepareWebcamThenStart());
    }

    void EnsureNoteUiVisible() {
        gameObject.SetActive(true);
        if (noteboxHead != null)
            noteboxHead.gameObject.SetActive(true);
        if (Slice1 != null)
            Slice1.gameObject.SetActive(true);

        var noteFrame = GetComponent<Image>();
        if (noteFrame != null)
            noteFrame.enabled = true;

        // Webcam is sibling index 0 under LV3Play; keep note UI above it.
        transform.SetAsLastSibling();
        if (nextButton != null)
            nextButton.transform.SetAsLastSibling();
        if (dsm != null && dsm.ReturnButton != null)
            dsm.ReturnButton.transform.SetAsLastSibling();
    }

    IEnumerator PrepareWebcamThenStart() {
        MBodyDiagLog.Step("NoteManager",
            $"Preparing LV3 play GameLevel={(dsm != null ? dsm.GameLevel : -1)} danceNum={(dm != null ? dm.danceNum : -1)}");

        yield return null;
        ResolveRefs();
        if (dsm != null)
            dsm.RefreshNoteTypes();

        yield return BodyResourceLifecycle.EnsureSmallPlateWebcamReady(false, transform);
        EnsureNoteUiVisible();

        webcamRoutine = null;

        if (!isActiveAndEnabled) {
            MBodyDiagLog.Warn("NoteManager", "Skip startNote; NoteManager disabled");
            yield break;
        }

        startNote();
    }

    IEnumerator createNotes(float term, float speed) {
        ResolveRefs();
        if (nextButton != null)
            nextButton.SetActive(false);

        if (dm == null || dm.danceNum <= 0) {
            MBodyDiagLog.Error("NoteManager", $"Cannot start danceNum={dm?.danceNum ?? 0}");
            if (txt != null)
                txt.text = "댄스를 먼저 선택해 주세요.";
            ShowNextButton();
            yield break;
        }

        if (dsm != null)
            dsm.RefreshNoteTypes();

        int gameLevel = dsm != null ? dsm.GameLevel : 1;
        MBodyDiagLog.Step("NoteManager",
            $"createNotes dance={dm.danceNum} gameLevel={gameLevel} speed={speed:F2} term={term:F2} bgms={(bgms != null ? bgms.Length : 0)}");

        if (noteTypes == null || noteTypes.Length == 0) {
            MBodyDiagLog.Error("NoteManager", "noteTypes empty");
            ShowNextButton();
            yield break;
        }

        if (auso == null) {
            MBodyDiagLog.Error("NoteManager", "AudioSource missing");
            ShowNextButton();
            yield break;
        }

        auso.mute = false;
        auso.volume = 1f;
        auso.pitch = speed;
        auso.playOnAwake = false;
        auso.loop = false;

        // Start stage-3 recording only after webcam has a real frame (avoids encoder init crash).
        if (gameLevel > 2 && vcc != null) {
            vcc.prefix = "DanceThirdPlay";
            if (vcc.wci == null || !vcc.wci.HasValidFrame) {
                MBodyDiagLog.Warn("NoteManager", "Delaying stage-3 record until webcam frame is ready");
                for (int i = 0; i < 60 && (vcc.wci == null || !vcc.wci.HasValidFrame); i++)
                    yield return null;
            }
            try {
                if (!vcc.IsRecording)
                    vcc.startRecord();
            } catch (System.Exception ex) {
                MBodyDiagLog.Exception("NoteManager", "startRecord", ex);
            }
        }

        tr1 = term * 2 * 1 / 8f;
        tr2 = term * 2 * 7f / 8f;
        tr3 = term * 2 * 7f / 16f;

        if (txt != null)
            txt.text = MBodyKoreanText.DanceSectionIntro;

        int count = 0;
        int[] noteSequence = {
            0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 1, 1,
            0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0
        };

        SpawnNote(noteSequence, ref count);
        yield return new WaitForSecondsRealtime(1f);

        if (txt != null)
            txt.text = MBodyKoreanText.DanceSectionPlay;

        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, term - 1f));

        var clip = ResolveBgmClip(dm.danceNum, gameLevel);
        float clipLength;
        if (clip != null) {
            auso.clip = clip;
            auso.pitch = speed;
            auso.Play();
            // Pitched playback finishes sooner: real duration = length / pitch.
            float naturalLen = clip.length > 0.1f ? clip.length : term * 24f * speed;
            clipLength = naturalLen / Mathf.Max(0.01f, speed);
            MBodyDiagLog.Step("NoteManager",
                $"Playing bgm '{clip.name}' naturalLen={naturalLen:F1}s playLen={clipLength:F1}s pitch={speed:F2} level={gameLevel}");
        } else {
            clipLength = term * 24f;
            MBodyDiagLog.Warn("NoteManager",
                $"No BGM clip for dance={dm.danceNum} level={gameLevel}; notes continue without audio");
        }

        // Drive notes by realtime clock; never end before the pitched music finishes.
        float startRt = Time.realtimeSinceStartup;
        while (count < 24 && _Play) {
            float elapsed = Time.realtimeSinceStartup - startRt;
            if (elapsed >= (count - 1) * term)
                SpawnNote(noteSequence, ref count);
            yield return null;
        }

        float elapsedNow = Time.realtimeSinceStartup - startRt;
        float waitMusic = clipLength - elapsedNow;
        if (waitMusic > 0f)
            yield return new WaitForSecondsRealtime(waitMusic);

        if (auso != null && auso.isPlaying) {
            while (auso.isPlaying && _Play)
                yield return null;
        }

        if (gameLevel > 2 && vcc != null) {
            try {
                if (vcc.IsRecording)
                    vcc.stopRecording(true);
            } catch (System.Exception ex) {
                MBodyDiagLog.Exception("NoteManager", "stopRecording", ex);
            }

            ApplyDanceStarClear();
        }

        // Let the last notes finish their travel animation.
        yield return new WaitForSecondsRealtime(Mathf.Min(term * 2f, 3f));
        _Play = false;

        if (auso != null) {
            if (auso.isPlaying)
                auso.Stop();
            auso.pitch = 1f;
        }

        FinishStage(gameLevel, count);
        noteRoutine = null;
    }

    void FinishStage(int gameLevel, int notesSpawned) {
        MBodyDiagLog.Step("NoteManager",
            $"Stage complete gameLevel={gameLevel} notesSpawned={notesSpawned}");

        // Stage 3 is the final exam — go to LV3ThirdEnd, not the 1/2-stage retry menu (LV3End).
        if (gameLevel >= 3) {
            if (nextButton != null)
                nextButton.SetActive(false);
            if (dsm != null && dsm.ReturnButton != null)
                dsm.ReturnButton.gameObject.SetActive(false);

            var host = BodyResourceLifecycle.FindSceneFlowHost(transform);
            var selfPage = host != null ? host.gameObject : gameObject;
            var thirdEnd = SceneFlowRegistry.Get("LV3ThirdEnd");
            if (thirdEnd != null) {
                MBodyDiagLog.Step("NoteManager", "Stage 3 complete -> LV3ThirdEnd");
                SceneFlowNavigator.ShowOnly(thirdEnd, selfPage);
                return;
            }
        }

        ShowNextButton();
    }

    AudioClip ResolveBgmClip(int danceNum, int gameLevel) {
        if (bgms == null || bgms.Length == 0)
            return null;

        int baseIndex = DanceBgmBase[Mathf.Clamp(danceNum - 1, 0, DanceBgmBase.Length - 1)];

        // Prefer per-level clip when authored; otherwise fall back to the dance base track.
        for (int level = gameLevel; level >= 1; level--) {
            int idx = baseIndex + (level - 1);
            if (idx >= 0 && idx < bgms.Length && bgms[idx] != null) {
                MBodyDiagLog.Step("NoteManager", $"BGM resolve base={baseIndex} tryLevel={level} -> index={idx}");
                return bgms[idx];
            }
        }

        if (baseIndex >= 0 && baseIndex < bgms.Length)
            return bgms[baseIndex];
        return null;
    }

    void ApplyDanceStarClear() {
        if (dm == null || string.IsNullOrEmpty(dm.danceStar) || dm.danceStar.Length < 14)
            return;

        int idx = dm.danceNum - 1;
        if (idx < 0 || idx > 5)
            return;

        string temp = dm.danceStar;
        char[] tx = new char[6];
        for (int i = 0; i < 6; i++)
            tx[i] = temp[i];
        if (tx[idx] == 'F')
            tx[idx] = 'T';
        dm.danceStar = string.Concat(tx[0], tx[1], tx[2], tx[3], tx[4], tx[5], temp[6],
            temp[7], temp[8], temp[9], temp[10], temp[11], temp[12], temp[13]);
        if (dm.fileManager != null) {
            dm.fileManager.danceStar = dm.danceStar;
            dm.fileManager.UpdateResult();
            dm.fileManager.WriteResult("Dance,3," + "0," + "DanceIndex:" + dm.danceNum);
        }
    }

    void ShowNextButton() {
        if (nextButton != null) {
            nextButton.SetActive(true);
            nextButton.transform.SetAsLastSibling();
        }
    }

    static void ApplyNoteSpriteSize(Image img, float scale) {
        img.SetNativeSize();
        img.rectTransform.sizeDelta = img.rectTransform.sizeDelta * scale;
    }

    Sprite[] ResolvePersonSprites() {
        // T1 holds per-dance person photos (6 dances × 2 poses). T2/T3 in the scene are only
        // square placeholders (2 and 1 sprites) — never use those for the scrolling pose strip.
        if (dsm != null && dsm.T1 != null && dsm.T1.Length > 0)
            return dsm.T1;
        return noteTypes;
    }

    void SpawnNote(int[] noteSequence, ref int count) {
        if (count < 0 || count >= noteSequence.Length)
            return;
        if (Slice1 == null || noteboxHead == null)
            return;

        var sprites = ResolvePersonSprites();
        if (sprites == null || sprites.Length == 0)
            return;

        int gameLevel = dsm != null ? dsm.GameLevel : 1;
        // Soft/power pose pair for the selected dance: index 0/1 within that dance's slot.
        int spriteIndex = noteSequence[count] + (dm.danceNum - 1) * 2;
        if (spriteIndex < 0 || spriteIndex >= sprites.Length || sprites[spriteIndex] == null) {
            MBodyDiagLog.Warn("NoteManager",
                $"Skip note spriteIndex={spriteIndex} types={sprites.Length} dance={dm.danceNum} level={gameLevel}");
            count++;
            return;
        }

        GameObject gm = Instantiate(Slice1.gameObject, noteboxHead);
        gm.SetActive(true);
        gm.name = $"Note_{count}_L{gameLevel}_s{spriteIndex}";
        Image img = gm.GetComponent<Image>();
        if (img == null) {
            Destroy(gm);
            count++;
            return;
        }

        img.sprite = sprites[spriteIndex];
        img.enabled = true;
        img.color = Color.white;
        // All stages use the same photo size progression as level 1.
        ApplyNoteSpriteSize(img, 0.25f);

        // Original order: newest note at sibling 0 (behind). First/larger notes stay in front.
        img.transform.SetSiblingIndex(0);
        StartCoroutine(noteLife(img));
        count++;
        if (count == 1 || count == 24)
            MBodyDiagLog.Step("NoteManager",
                $"Spawn note#{count} sprite={sprites[spriteIndex].name} level={gameLevel} dance={dm.danceNum}");
    }

    IEnumerator noteLife(Image img) {
        if (img == null)
            yield break;

        // Same grow/travel sizing as level 1 for every stage.
        Vector2 original = img.rectTransform.sizeDelta * 0.25f;
        float startTime = Time.realtimeSinceStartup;

        while (img != null) {
            float ratio = (Time.realtimeSinceStartup - startTime) / Mathf.Max(0.01f, tr1);
            img.rectTransform.sizeDelta = original * 0.25f * (1 - ratio) + original * 2f * ratio;
            img.rectTransform.anchoredPosition =
                Slice1.anchoredPosition * (1 - ratio) + Slice2.anchoredPosition * ratio;
            img.color = new Color(1f, 1f, 1f, Mathf.Clamp01(ratio));
            if (ratio >= 1f)
                break;
            if (!_Play) {
                Destroy(img.gameObject);
                yield break;
            }
            yield return null;
        }

        startTime = Time.realtimeSinceStartup;
        while (img != null) {
            float ratio = (Time.realtimeSinceStartup - startTime) / Mathf.Max(0.01f, tr2);
            img.rectTransform.sizeDelta = original * 2f * (1 - ratio) + original * 6f * ratio;
            img.rectTransform.anchoredPosition =
                Slice2.anchoredPosition * (1 - ratio) + Slice3.anchoredPosition * ratio;
            if (ratio >= 1f)
                break;
            if (!_Play) {
                Destroy(img.gameObject);
                yield break;
            }
            yield return null;
        }

        startTime = Time.realtimeSinceStartup;
        while (img != null) {
            float ratio = (Time.realtimeSinceStartup - startTime) / Mathf.Max(0.01f, tr3);
            img.rectTransform.sizeDelta = original * 6f * (1 - ratio) + original * 7.3126f * ratio;
            img.rectTransform.anchoredPosition =
                Slice3.anchoredPosition * (1 - ratio) + Slice4.anchoredPosition * ratio;
            img.color = new Color(1f, 1f, 1f, 1f - 0.7f * Mathf.Clamp01(ratio));
            if (ratio >= 1f)
                break;
            if (!_Play) {
                Destroy(img.gameObject);
                yield break;
            }
            yield return null;
        }

        if (img != null)
            Destroy(img.gameObject);
    }

    void OnDisable() {
        _Play = false;
        if (webcamRoutine != null) {
            StopCoroutine(webcamRoutine);
            webcamRoutine = null;
        }
        if (noteRoutine != null) {
            StopCoroutine(noteRoutine);
            noteRoutine = null;
        }

        // Abort in-progress stage-3 encode if the page is closed mid-song (prevents leaked recorders).
        if (vcc != null && vcc.IsRecording) {
            try {
                vcc.stopRecording(false, 0);
            } catch (System.Exception ex) {
                MBodyDiagLog.Exception("NoteManager", "stopRecording on disable", ex);
            }
        }

        StopAllCoroutines();
        if (noteboxHead != null) {
            for (int i = noteboxHead.childCount - 1; i >= 0; i--)
                Destroy(noteboxHead.GetChild(i).gameObject);
        }
        if (auso != null) {
            if (auso.isPlaying)
                auso.Stop();
            auso.pitch = 1f;
        }

        BodyResourceLifecycle.PrepareForPageTransition();
    }
}
