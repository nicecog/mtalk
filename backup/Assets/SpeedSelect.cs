using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class SpeedSelect : MonoBehaviour
{
    [Range(0, 3)] public int level = 0;

    public FlowManager FM;
    // Start is called before the first frame update
    void Awake()
    {
        FM = FindFirstObjectByType<FlowManager>();
    }

    private void OnEnable()
    {
        level = 2;
        clickLevel(2);
    }

    public RectTransform[] Knobs;
    public void clickLevel(int lev)
    {
        level = lev;
        nextButton.SetActive(true);
        FM.level = lev;
        onObject.gameObject.SetActive(true);
        onObject.rectTransform.anchoredPosition = new Vector2(Knobs[lev - 1].anchoredPosition.x,Knobs[lev-1].anchoredPosition.y);
    }
    public Image onObject;
    public GameObject nextButton;
}
