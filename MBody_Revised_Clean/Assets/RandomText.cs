using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomText : MonoBehaviour {
    public string[] strs;

    private void Awake() {
        txt = GetComponent<Text>();
    }

    private Text txt;
    private void OnEnable() {
        int t = UnityEngine.Random.Range(0, strs.Length);
        MBodyKoreanText.EnsureKoreanFont(txt);
        txt.text = strs[t];

    }
}
