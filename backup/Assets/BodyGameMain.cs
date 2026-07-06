using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyGameMain : MonoBehaviour
{
    public BodyManager bm;

    void OnEnable() {
        StartCoroutine(BeginMainGame());
    }

    IEnumerator BeginMainGame() {
        yield return null;
        yield return null;

        bm = FindFirstObjectByType<BodyManager>();
        if (bm == null) {
            MBodyDiagLog.Warn("BodyGame", "BodyGameMain missing BodyManager");
            yield break;
        }

        bm.playTimes++;
        if (bm.pds != null)
            bm.pds.setNumber(bm.playTimes);

        var bgs = GetComponent<BodyGameScene>();
        if (bgs == null) {
            MBodyDiagLog.Warn("BodyGame", "BodyGameMain missing BodyGameScene");
            yield break;
        }

        MBodyDiagLog.Step("BodyGame", $"BeginMainGame playTimes={bm.playTimes}");
        bgs.startGame();
    }
}
