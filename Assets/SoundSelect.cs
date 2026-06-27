using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundSelect : MonoBehaviour {
    public GameObject[] Icons;
    public bool isAfter4 = false;
    private Button NextButton;
    public AudioClip[] audio_sources;
    public bool isPlaying = false;
    public DragSlot[] slots;

    private readonly int[] lengths_sources = {
        1, 3, 4, 6, 2, 2, 4, 7, 1, 2, 3, 1, 2, 7, 2, 1, 4,
        4, 6, 2, 5, 4, 3, 2, 2, 4, 2, 5, 3, 3, 1, 2
    };

    private BodyManager bm;
    private AudioSource auso;

    public MusicSelect ms;
    public Image Pop;

    private void Awake() {
        for (int i = 0; i < Icons.Length; i++) {
            int temp = i;
            Icons[i].transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => PlaySound(temp));
        }

        auso = GetComponent<AudioSource>();
    }

    private IEnumerator MusicOn() {
        yield return new WaitForSecondsRealtime(1f);
        if (!isActiveAndEnabled)
            yield break;

        bm = FindFirstObjectByType<BodyManager>(FindObjectsInactive.Include);
        if (bm == null) {
            MBodyDiagLog.Warn("SoundSelect", $"{name} MusicOn: BodyManager not found");
            yield break;
        }

        var bodyAudio = bm.GetComponent<AudioSource>();
        if (bodyAudio == null) {
            MBodyDiagLog.Warn("SoundSelect", $"{name} MusicOn: BodyManager AudioSource missing");
            yield break;
        }

        var previewClips = gameObject.name == "SoundSelectYellow" ? bm.preSoundsA : bm.preSoundsB;
        if (previewClips == null || bm.song_idx < 0 || bm.song_idx >= previewClips.Length) {
            MBodyDiagLog.Warn("SoundSelect", $"{name} MusicOn: invalid preview song_idx={bm.song_idx}");
            yield break;
        }

        bodyAudio.clip = previewClips[bm.song_idx];
        bodyAudio.Play();
    }

    private int FullSlots = 0;

    void Update() {
        bool SlotChanged = false;
        int[] nowSlot = new int[slots.Length];
        for (int i = 0; i < slots.Length; i++) {
            nowSlot[i] = slots[i].indexData;
            if (nowSlot[i] != slotData[i])
                SlotChanged = true;
        }
        slotData = nowSlot;
        if (SlotChanged) {
            int slotsFull = 0;
            foreach (var v in slots) {
                if (v.isFull)
                    slotsFull++;
            }

            if (slotsFull >= 2) {
                NextButton.interactable = true;
                var isBlue = name == "SoundSelectBlue";
                StartCoroutine(popUpCoroutine(MBodyKoreanText.InstrumentPickComplete(isBlue)));
                if (bm == null)
                    bm = FindFirstObjectByType<BodyManager>();
                if (bm != null)
                    bm.CacheInstrumentPicks();
            }
            else {
                NextButton.interactable = false;
                if (slotsFull > FullSlots) {
                    var t = UnityEngine.Random.Range(0, 9);
                    StartCoroutine(popUpCoroutine(MBodyKoreanText.InstrumentPickFeedback[t]));
                }
            }

            FullSlots = slotsFull;
        }
    }

    private IEnumerator popUpCoroutine(string msg) {
        Text txt = Pop.transform.GetChild(0).GetComponent<Text>();
        MBodyKoreanText.EnsureKoreanFont(txt);
        txt.text = msg;
        Pop.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(1f);
        Pop.gameObject.SetActive(false);
    }

    private int[] slotData;
    private Vector3[] IconOriginalPositions;
    private void OnEnable() {
        StopAllCoroutines();
        IconOriginalPositions = new Vector3[Icons.Length];
        for (int i = 0; i < Icons.Length; i++) {
            IconOriginalPositions[i] = Icons[i].transform.position;
            if (Icons[i].transform.childCount > 0) {
                Icons[i].transform.GetChild(0).localPosition = Vector3.zero;
                var drag = Icons[i].transform.GetChild(0).GetComponent<DragIcon>();
                if (drag != null)
                    drag.SyncIndexDataFromParent();
            }
        }

        ResetAllSlots();
        StartCoroutine(MusicOn());
        slotData = new int[slots.Length];
        FullSlots = 0;
        NextButton = transform.Find("NextButton").GetComponent<Button>();
        NextButton.interactable = false;
        isPlaying = false;
        Pop.gameObject.SetActive(false);
    }

    void ResetAllSlots()
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++) {
            if (slots[i] == null)
                continue;

            slots[i].resetIcon();
            if (slots[i].backIcons == null)
                continue;

            var drop = slots[i].backIcons.GetComponent<DropIcon>();
            if (drop != null && drop.isDropped)
                drop.ResetPosition();
        }
    }

    private void OnDisable() {
        StopAllCoroutines();
        isPlaying = false;

        if (bm == null)
            bm = FindFirstObjectByType<BodyManager>(FindObjectsInactive.Include);
        if (bm != null)
            bm.CacheInstrumentPicks();

        if (bm != null) {
            var bodyAudio = bm.GetComponent<AudioSource>();
            if (bodyAudio != null)
                bodyAudio.Stop();
        }

        Pop.gameObject.SetActive(false);
        if (IconOriginalPositions != null) {
            for (int i = 0; i < Icons.Length && i < IconOriginalPositions.Length; i++) {
                Icons[i].transform.position = IconOriginalPositions[i];
                if (Icons[i].transform.childCount > 0)
                    Icons[i].transform.GetChild(0).localPosition = Vector3.zero;
            }
        }
    }

    public void PlaySound(int index) {
        StartCoroutine(waitSound(index));
    }

    private IEnumerator waitSound(int index) {
        if (audio_sources == null || index < 0 || index >= audio_sources.Length || audio_sources[index] == null)
            yield break;

        isPlaying = false;
        auso.Stop();
        auso.clip = audio_sources[index];
        auso.Play();
        yield return new WaitForSecondsRealtime(0.1f);

        float startTime = Time.fixedTime;
        isPlaying = true;
        var duration = index < lengths_sources.Length ? lengths_sources[index] : 2f;
        while (isActiveAndEnabled && isPlaying && Time.fixedTime - startTime < duration)
            yield return null;

        isPlaying = false;
    }
}
