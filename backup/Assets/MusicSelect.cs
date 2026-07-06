using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MusicSelect : MonoBehaviour {
    // Start is called before the first frame update
    void Start() {
        ads = GetComponent<AudioSource>();
        bm = FindFirstObjectByType<BodyManager>();
        dm = FindFirstObjectByType<DanceManager>();
        isPlaying = false;
    }

    // Update is called once per frame
    void Update() {
        if (!isPlaying)
            playingIcon.enabled = false;
        else
            playingIcon.enabled = true;
    }

    private AudioSource ads;
    public bool isPlaying = false;
    public int nowPlaying = 0;
    public Image playingIcon;
    public void playPreSource(int source_idx) {
        if (!isPlaying) {
            nowPlaying = source_idx;
            ads.clip = name=="LV4MusicSelect"?bm.DKPreview[source_idx]:bm.BodyPreview[source_idx];
            float[] yCoord = name == "LV4MusicSelect"
                ? new float[7] {-172-0.6f, 548-0.6f, -352-0.6f, 8-0.6f, 368-0.6f, -532-0.6f, 188-0.6f}
                : new float[6] {464f+10,284f+10,104f+10,-76f+10,-256f+10,-436f+10 };
            playingIcon.rectTransform.anchoredPosition = new Vector2(playingIcon.rectTransform.anchoredPosition.x,yCoord[source_idx]);
            ads.Play();
            isPlaying = true;
        }
        else {
            if (source_idx == nowPlaying) {
                isPlaying = false;
                ads.Stop();
            }
            else {
                ads.Stop();
                nowPlaying = source_idx;
                ads.clip = name=="LV4MusicSelect"?bm.DKPreview[source_idx]:bm.BodyPreview[source_idx];
                float[] yCoord = name == "LV4MusicSelect"
                    ? new float[7] {-172-0.6f, 548-0.6f, -352-0.6f, 8-0.6f, 368-0.6f, -532-0.6f, 188-0.6f}
                    : new float[6] {464f+10,284f+10,104f+10,-76f+10,-256f+10,-436f+10 };
                playingIcon.rectTransform.anchoredPosition = new Vector2(playingIcon.rectTransform.anchoredPosition.x,yCoord[source_idx]);
                ads.Play();
            }
        }
    }

    private BodyManager bm;
    private DanceManager dm;

    private void OnEnable() {
        isPlaying = false;
    }

    public void selectMusic(int source_idx) {
        if (name != "LV4MusicSelect") {
            if (source_idx < 0) {
                int max = dm.playTimes < 5 ? 3 : 6;
                source_idx = UnityEngine.Random.Range(0, max);
            }
            bm.song_idx = source_idx;
            bm.vcc.prefix = "BodyAlign_" + (bm.playTimes+1);
            int[] t= new int[8];
            if (source_idx == 0) {
                for (int i = 0; i < 8; i++) {
                    icons[i].sprite = tiles1[i];
                    icons[i].SetNativeSize();
                    t = new int[]{6,8,28,31,10,19,0,17};
                }
            }
            else if (source_idx == 1) {
                for (int i = 0; i < 8; i++) {
                    icons[i].sprite = tiles2[i];
                    icons[i].SetNativeSize();
                    t =new int[]{ 23,6,32,18,11,12,19,27 } ;
                }
            }
            else if (source_idx == 2) {
                for (int i = 0; i < 8; i++) {
                    icons[i].sprite = tiles3[i];
                    icons[i].SetNativeSize();
                    t =new int[]{ 6,8,29,30,26,16,25,20 } ;
                }
            }
            else if (source_idx == 3) {
                for (int i = 0; i < 8; i++) {
                    icons[i].sprite = tiles4[i];
                    icons[i].SetNativeSize();
                    t =new int[]{ 23, 18, 8,28, 11, 12, 0,25 } ;
                }
            }
            else if (source_idx == 4) {
                for (int i = 0; i < 8; i++) {
                    icons[i].sprite = tiles5[i];
                    icons[i].SetNativeSize();
                    t =new int[]{ 22, 18, 29, 23, 11, 14, 26,20 } ;
                }
            }
            else if (source_idx == 5) {
                for (int i = 0; i < 8; i++) {
                    icons[i].sprite = tiles6[i];
                    icons[i].SetNativeSize();
                    t =new int[]{ 22, 6, 28, 32, 17, 16,27,20 } ;
                }
            }
            else {
                MBodyDiagLog.Warn("MusicSelect", $"selectMusic unsupported source_idx={source_idx}");
                return;
            }

            if (!AssignInstrumentClips(t))
                MBodyDiagLog.Warn("MusicSelect", $"selectMusic clip assign failed song={source_idx}");
        }
        else {
            selectMusicForDance(source_idx);
        }
    }

    bool AssignInstrumentClips(int[] clipIndices)
    {
        if (clips == null || Yellow == null || Blue == null
            || Yellow.audio_sources == null || Blue.audio_sources == null
            || clipIndices == null || clipIndices.Length < 8)
            return false;

        if (!TryAssignClip(Yellow.audio_sources, 0, clipIndices[0])
            || !TryAssignClip(Yellow.audio_sources, 1, clipIndices[1])
            || !TryAssignClip(Yellow.audio_sources, 2, clipIndices[2])
            || !TryAssignClip(Yellow.audio_sources, 3, clipIndices[3])
            || !TryAssignClip(Blue.audio_sources, 0, clipIndices[4])
            || !TryAssignClip(Blue.audio_sources, 1, clipIndices[5])
            || !TryAssignClip(Blue.audio_sources, 2, clipIndices[6])
            || !TryAssignClip(Blue.audio_sources, 3, clipIndices[7]))
            return false;

        MBodyDiagLog.Step("MusicSelect", $"Assigned instrument clips for song_idx={bm.song_idx}");
        return true;
    }

    bool TryAssignClip(AudioClip[] targets, int slot, int clipIndex)
    {
        if (targets == null || slot < 0 || slot >= targets.Length
            || clips == null || clipIndex < 0 || clipIndex >= clips.Length)
            return false;

        targets[slot] = clips[clipIndex];
        return targets[slot] != null;
    }

    public void selectMusicForDance(int source_idx) {
        dm.song_idx = source_idx;
    }

    public AudioClip[] clips;
    public Sprite[] tiles1;
    public Sprite[] tiles2;
    public Sprite[] tiles3;
    public Sprite[] tiles4;
    public Sprite[] tiles5;
    public Sprite[] tiles6;

    public Image[] icons;

    public SoundSelect Yellow;
    public SoundSelect Blue;
}