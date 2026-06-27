using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LV3EndTextBar : MonoBehaviour
{
    public DanceStarManager dm;
    private Text txt;
    private void OnEnable() {
        txt = GetComponent<Text>();
        jump.SetActive(false);
        if (dm.GameLevel <= 2) {
            txt.text = "댄스 스타 " + dm.GameLevel + "단계가 쉬웠으면 다음 단계 도전,\n어려웠으면 재도전을 선택해 주세요.";
            jump.SetActive(true);
        }
    }

    public GameObject jump;

}
