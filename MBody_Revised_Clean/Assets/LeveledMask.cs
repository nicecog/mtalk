using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LeveledMask : MonoBehaviour
{
    public DanceManager dm;
    public GameObject[] masks;

    bool syncInFlight;

    void Awake() {
        dm = FindFirstObjectByType<DanceManager>();
    }

    void OnEnable() {
        if (dm == null)
            dm = FindFirstObjectByType<DanceManager>(FindObjectsInactive.Include);

        // DanceSelect may not always have DanceModePage in every pack layout.
        if (name == "DanceSelect" || name == "DanceLevelSelect") {
            if (GetComponent<DanceHomeButton>() == null)
                gameObject.AddComponent<DanceHomeButton>();
        }

        if (ShouldPullServerProgress()) {
            StartCoroutine(PullServerProgressThenApplyMasks());
            return;
        }

        ApplyMasks();
    }

    bool ShouldPullServerProgress() {
        return name == "DanceSelect" || name == "DanceLevelSelect" || name == "Result";
    }

    IEnumerator PullServerProgressThenApplyMasks() {
        if (syncInFlight)
            yield break;

        syncInFlight = true;
        try {
            var loginForm = dm != null ? dm.fileManager : null;
            if (loginForm == null)
                loginForm = FindFirstObjectByType<LoginForm>(FindObjectsInactive.Include);

            var jr = loginForm != null ? loginForm.jr : null;
            if (jr == null)
                jr = FindFirstObjectByType<JsonRequest>(FindObjectsInactive.Include);

            // Run on this active page so inactive LoginForm does not block the coroutine.
            if (jr != null && jr.HasSession && loginForm != null) {
                var progressOk = false;
                var progressError = "";
                PlayerProgressData progress = null;
                yield return jr.SyncMyProgress((ok, err, data) => {
                    progressOk = ok;
                    progressError = err;
                    progress = data;
                });

                if (progressOk && progress != null) {
                    loginForm.ApplyServerProgress(progress);
                    MBodyDiagLog.Step("LeveledMask",
                        $"{name}: server progress applied playTimes={progress.playTimes} danceStar={progress.danceStar}");
                } else {
                    MBodyDiagLog.Warn("LeveledMask",
                        $"{name}: server progress sync failed ({progressError}); using local state");
                }
            } else {
                MBodyDiagLog.Warn("LeveledMask", $"{name}: no login session; using local state");
            }

            ApplyMasks();
            RefreshProgressIcons();
        } finally {
            syncInFlight = false;
        }
    }

    void ApplyMasks() {
        if (masks == null || masks.Length == 0 || dm == null)
            return;

        dm.ResolvePackRefs();

        if (name == "MusicSelect") {
            foreach (var v in masks)
                v.SetActive(dm.playTimes < 5);
            return;
        }

        if (name == "DanceSelect" || name == "Result") {
            // masks[i] active = locked. Unlock by playTimes day gate OR danceStar certification (T).
            // Dance order: 0=날아봐, 1=날봐줘, 2=디스코, 3=연주, 4=스웨그, 5=풍차
            masks[0].SetActive(false);
            masks[3].SetActive(false);
            masks[1].SetActive(!IsDanceSelectable(1, 2));
            masks[4].SetActive(!IsDanceSelectable(4, 3));
            masks[2].SetActive(!IsDanceSelectable(2, 4));
            masks[5].SetActive(!IsDanceSelectable(5, 4));
            return;
        }

        if (name == "DanceLevelSelect") {
            // danceStar T means LV3 was cleared, so LV1-3 and LV4(king) should all be playable.
            var starCleared = dm.danceNum > 0 && IsDanceStarCleared(dm.danceNum - 1);

            if (starCleared) {
                masks[0].SetActive(false);
                masks[1].SetActive(false);
                masks[2].SetActive(false);
            } else if (dm.danceNum == 1) {
                masks[0].SetActive(false);
                masks[1].SetActive(false);
                masks[2].SetActive(dm.playTimes < 2);
            } else if (dm.danceNum == 2) {
                masks[0].SetActive(dm.playTimes < 3);
                masks[1].SetActive(dm.playTimes < 3);
                masks[2].SetActive(dm.playTimes < 4);
            } else if (dm.danceNum == 3) {
                masks[0].SetActive(dm.playTimes < 5);
                masks[1].SetActive(dm.playTimes < 5);
                masks[2].SetActive(dm.playTimes < 7);
            } else if (dm.danceNum == 4) {
                masks[0].SetActive(false);
                masks[1].SetActive(false);
                masks[2].SetActive(dm.playTimes < 3);
            } else if (dm.danceNum == 5) {
                masks[0].SetActive(dm.playTimes < 4);
                masks[1].SetActive(dm.playTimes < 4);
                masks[2].SetActive(dm.playTimes < 5);
            } else if (dm.danceNum == 6) {
                masks[0].SetActive(dm.playTimes < 5);
                masks[1].SetActive(dm.playTimes < 6);
                masks[2].SetActive(dm.playTimes < 8);
            }

            if (masks.Length > 3)
                masks[3].SetActive(!starCleared);
        }
    }

    /// <summary>
    /// Dance is selectable when the login-day gate is met, or when that dance already has
    /// danceStar certification (admin portal or in-game clear).
    /// </summary>
    bool IsDanceSelectable(int danceIndex0, int minPlayTimesExclusive) {
        if (dm.playTimes > minPlayTimesExclusive)
            return true;
        return IsDanceStarCleared(danceIndex0);
    }

    bool IsDanceStarCleared(int danceIndex0) {
        var ds = dm.danceStar;
        if (string.IsNullOrEmpty(ds) || danceIndex0 < 0 || danceIndex0 >= Mathf.Min(6, ds.Length))
            return false;
        return ds[danceIndex0] != 'F';
    }

    void RefreshProgressIcons() {
        var stars = GetComponentsInChildren<StarIcon>(true);
        for (var i = 0; i < stars.Length; i++) {
            if (stars[i] != null)
                stars[i].Refresh();
        }

        var danceIcons = GetComponentsInChildren<ShowDanceIcons>(true);
        for (var i = 0; i < danceIcons.Length; i++) {
            if (danceIcons[i] != null)
                danceIcons[i].Refresh();
        }
    }
}
