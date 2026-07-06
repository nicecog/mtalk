using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DanceStarManager : MonoBehaviour
{
    // Start is called before the first frame update

    public Sprite[] T1, T2, T3;
    public NoteManager nm;

    public int GameLevel = 1;

    void OnEnable() {
        if (nm == null)
            nm = FindFirstObjectByType<NoteManager>(FindObjectsInactive.Include);
        SyncNoteTypes();
    }

    void Update()
    {
        if (nm == null)
            return;

        if (GameLevel >= 3 && nm._Play == false)
            ReturnButton.gameObject.SetActive(true);
        else
            ReturnButton.gameObject.SetActive(false);
    }

    public void setLevel(int t) {
        GameLevel = t;
        RefreshNoteTypes();
        MBodyDiagLog.Step("DanceStar", $"setLevel GameLevel={GameLevel} noteSprites={(nm?.noteTypes?.Length ?? 0)}");
    }

    public void RefreshNoteTypes() {
        if (nm == null)
            nm = FindFirstObjectByType<NoteManager>(FindObjectsInactive.Include);
        if (nm == null)
            return;

        // Always bind person photos (T1: 6 dances × soft/power pose). T2/T3 are square
        // placeholders only (2 / 1 sprites) and must not replace the pose strip.
        if (T1 == null || T1.Length == 0) {
            MBodyDiagLog.Warn("DanceStar", "T1 person sprites missing");
            return;
        }

        nm.noteTypes = T1;
        MBodyDiagLog.Step("DanceStar",
            $"RefreshNoteTypes GameLevel={GameLevel} personSprites={T1.Length} (T2={T2?.Length ?? 0} T3={T3?.Length ?? 0} unused squares)");
    }

    void SyncNoteTypes() => RefreshNoteTypes();

    public Button ReturnButton;

    public void NextLevel() {
        // Stage 3 is final — do not loop back into LV3Calib via LV3End "다음 단계 도전".
        if (GameLevel >= 3) {
            MBodyDiagLog.Step("DanceStar", "NextLevel ignored; stage 3 already finished");
            var thirdEnd = SceneFlowRegistry.Get("LV3ThirdEnd");
            if (thirdEnd != null)
                SceneFlowNavigator.ShowOnly(thirdEnd, gameObject);
            return;
        }

        if (GameLevel == 1)
            setLevel(2);
        else if (GameLevel == 2)
            setLevel(3);

        PrepareForCalib();
    }

    public void RestartLevel() {
        setLevel(GameLevel);
        PrepareForCalib();
    }

    void PrepareForCalib() {
        MBodyDiagLog.Step("DanceStar", $"PrepareForCalib GameLevel={GameLevel}");
        // LV3Calib.OnEnable will attach/start the webcam; only restore layout hints here.
        var flow = FindFirstObjectByType<FlowManager>(FindObjectsInactive.Include);
        if (flow != null && flow.WebCamObject != null) {
            flow.WebCamObject.gameObject.SetActive(true);
            flow.WebCamObject.raycastTarget = false;
            if (flow.WebCamObject.texture != null)
                flow.WebCamObject.enabled = true;
        }
    }

}
