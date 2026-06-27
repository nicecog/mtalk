using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DanceManager : MonoBehaviour {
    // Start is called before the first frame update
    void Start() {
        ResolvePackRefs();
        time = 0;
        DanceTiming = false;
        StartCoroutine(Check());
        
    }

    public void ResolvePackRefs()
    {
        if (timeOverButton == null)
        {
            var danceSelect = SceneFlowRegistry.Get("DanceSelect");
            if (danceSelect != null)
                timeOverButton = danceSelect.GetComponentInChildren<Button>(true);
        }
    }

    public void ResetDanceManager() {
        danceNum = 0;
        level = 0;
        speed = 0;
    }

    public int danceNum { get; set; }

    public int level { get; set; }

    public int speed { get; set; }


    public int song_idx;
    public int playTimes;

    public string danceStar {
        get;
        set;
    }

    public int time {
        get;
        set;
    }

    public bool DanceTiming {
        set;
        get;
    }
    IEnumerator Check() {
        yield return new WaitForSecondsRealtime(1f);
        if (DanceTiming) time++;
        StartCoroutine(Check());
        int pt = playTimes == 1 ? 12 * 60 : playTimes < 4 ? 12 * 60 : playTimes < 6 ? 17 * 60 : 20 * 60;
        if (timeOverButton != null)
            timeOverButton.gameObject.SetActive(time>=pt);
        
    }

    public LoginForm fileManager;
    public string ID;
    public Button timeOverButton;
}

