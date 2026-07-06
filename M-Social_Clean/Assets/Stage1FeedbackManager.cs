using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stage1FeedbackManager : MonoBehaviour {
    public int type;

    public void startFeedback(int stageType) {
        type = stageType;
        if (FeedbackPage == null || FeedbackPage.Length == 0) {
            MSocialStartupLog.Log("Stage1FeedbackManager missing FeedbackPage");
            return;
        }
        if (stageType < 0 || stageType >= FeedbackPage.Length) {
            MSocialStartupLog.Log("Stage1FeedbackManager invalid type=" + stageType + " len=" + FeedbackPage.Length);
            return;
        }
        foreach (var i in FeedbackPage) {
            if (i != null)
                i.SetActive(false);
        }
        FeedbackPage[type].SetActive(true);
        MSocialStartupLog.Log("Stage1FeedbackManager show page=" + type + " name=" + FeedbackPage[type].name);
    }
    private void OnEnable() {
        
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public GameObject[] FeedbackPage;
    
}
