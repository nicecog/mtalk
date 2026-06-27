using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LV3TextBar : MonoBehaviour
{
    public DanceStarManager dm;
    private Text txt;

    private void OnEnable() {
        txt = GetComponent<Text>();
        txt.text = "뒤로 물러나 안내선에 몸을 맞추면 " + dm.GameLevel + "단계 도전을 시작합니다";
    }
}
