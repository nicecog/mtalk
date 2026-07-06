using System;
using System.Collections;
using UnityEngine;

public class LV4PlayGame : MonoBehaviour
{
    public AudioClip[] bgms;
    public DanceManager dm;
    public VideoCaptureCam vcc;
    public GameObject nextButton;

    AudioSource auso;
    Coroutine gameRoutine;

    void Awake() {
        auso = GetComponent<AudioSource>();
        ResolveRefs();
    }

    void ResolveRefs() {
        if (dm == null)
            dm = FindFirstObjectByType<DanceManager>(FindObjectsInactive.Include);
        if (vcc == null)
            vcc = FindFirstObjectByType<VideoCaptureCam>(FindObjectsInactive.Include);
        if (auso == null)
            auso = GetComponent<AudioSource>();
        if (dm != null && dm.fileManager == null)
            dm.fileManager = FindFirstObjectByType<LoginForm>(FindObjectsInactive.Include);
    }

    void OnEnable() {
        ResolveRefs();
        if (gameRoutine != null)
            StopCoroutine(gameRoutine);
        gameRoutine = StartCoroutine(StartGame());
    }

    void OnDisable() {
        if (gameRoutine != null) {
            StopCoroutine(gameRoutine);
            gameRoutine = null;
        }
        if (auso != null && auso.isPlaying)
            auso.Stop();
        if (vcc != null && vcc.IsRecording)
            vcc.stopRecording(false, 0);

        BodyResourceLifecycle.PrepareForPageTransition();
    }

    public void setSong() {
        ResolveRefs();
        if (!TryAssignSongClip())
            MBodyDiagLog.Warn("LV4Play", "setSong failed; clip not assigned");
    }

    bool TryAssignSongClip() {
        if (auso == null || dm == null || bgms == null || bgms.Length == 0) {
            MBodyDiagLog.Warn("LV4Play",
                $"TryAssignSongClip missing refs auso={(auso != null)} dm={(dm != null)} bgms={(bgms != null ? bgms.Length : 0)}");
            return false;
        }

        // level: 1=soft(0), 2=mid(1), 3=power(2) — same as original.
        int speedSlot = dm.level == 1 ? 0 : dm.level == 3 ? 2 : 1;
        int songIdx = Mathf.Clamp(dm.song_idx, 0, 6);
        int clipIndex = songIdx * 3 + speedSlot;
        if (clipIndex < 0 || clipIndex >= bgms.Length || bgms[clipIndex] == null) {
            MBodyDiagLog.Warn("LV4Play",
                $"Invalid clipIndex={clipIndex} song_idx={dm.song_idx} level={dm.level} bgms={bgms.Length}");
            // Fall back to first available clip for this song.
            for (int i = songIdx * 3; i < songIdx * 3 + 3 && i < bgms.Length; i++) {
                if (bgms[i] != null) {
                    clipIndex = i;
                    break;
                }
            }
            if (clipIndex < 0 || clipIndex >= bgms.Length || bgms[clipIndex] == null)
                return false;
        }

        auso.clip = bgms[clipIndex];
        auso.pitch = 1f;
        auso.mute = false;
        auso.volume = 1f;
        MBodyDiagLog.Step("LV4Play",
            $"Song assigned clip='{auso.clip.name}' index={clipIndex} song_idx={dm.song_idx} level={dm.level}");
        return true;
    }

    IEnumerator StartGame() {
        MBodyDiagLog.Step("LV4Play", "StartGame begin");
        ResolveRefs();

        if (nextButton != null)
            nextButton.SetActive(false);

        if (dm == null) {
            MBodyDiagLog.Error("LV4Play", "DanceManager missing");
            ShowNext();
            yield break;
        }

        if (!TryAssignSongClip()) {
            MBodyDiagLog.Error("LV4Play", "No BGM clip; cannot start Dance King play");
            ShowNext();
            yield break;
        }

        // Keep self-cam visible on the right during play.
        yield return BodyResourceLifecycle.EnsureSmallPlateWebcamReady(true, transform);

        if (vcc != null) {
            vcc.prefix = "DanceKing";
            if (vcc.wci == null || !vcc.wci.HasValidFrame) {
                for (int i = 0; i < 60 && (vcc.wci == null || !vcc.wci.HasValidFrame); i++)
                    yield return null;
            }
            try {
                if (!vcc.IsRecording)
                    vcc.startRecord();
            } catch (Exception ex) {
                MBodyDiagLog.Exception("LV4Play", "startRecord", ex);
            }
        } else {
            MBodyDiagLog.Warn("LV4Play", "VideoCaptureCam missing; play continues without recording");
        }

        yield return new WaitForSecondsRealtime(1f);

        auso.Play();
        MBodyDiagLog.Step("LV4Play",
            $"Playing DanceKing song_idx={dm.song_idx} len={(auso.clip != null ? auso.clip.length : 0f):F1}s");

        if (!auso.isPlaying) {
            MBodyDiagLog.Error("LV4Play", "AudioSource failed to play");
            FinishPlay(false);
            yield break;
        }

        while (auso.isPlaying)
            yield return null;

        FinishPlay(true);
        gameRoutine = null;
    }

    void FinishPlay(bool success) {
        if (vcc != null && vcc.IsRecording) {
            try {
                vcc.stopRecording(true);
            } catch (Exception ex) {
                MBodyDiagLog.Exception("LV4Play", "stopRecording", ex);
            }
        }

        if (success && dm != null)
            ApplyDanceKingClear();

        ShowNext();
        MBodyDiagLog.Step("LV4Play", $"FinishPlay success={success}");
    }

    void ApplyDanceKingClear() {
        if (dm == null || string.IsNullOrEmpty(dm.danceStar) || dm.danceStar.Length < 14)
            return;

        int idx = Mathf.Clamp(dm.song_idx, 0, 6);
        string temp = dm.danceStar;
        char[] tx = new char[7];
        for (int i = 7; i < 14; i++)
            tx[i - 7] = temp[i];
        tx[idx] = 'K';
        dm.danceStar = string.Concat(
            temp[0], temp[1], temp[2], temp[3], temp[4], temp[5], temp[6],
            tx[0], tx[1], tx[2], tx[3], tx[4], tx[5], tx[6]);

        if (dm.fileManager != null) {
            dm.fileManager.danceStar = dm.danceStar;
            dm.fileManager.UpdateResult();
            dm.fileManager.WriteResult("Dance,4,Song:" + (dm.song_idx + 1) + ",N/A");
        } else {
            MBodyDiagLog.Warn("LV4Play", "LoginForm/fileManager missing; danceStar not saved");
        }
    }

    void ShowNext() {
        if (nextButton != null) {
            nextButton.SetActive(true);
            nextButton.transform.SetAsLastSibling();
        }
    }
}
