using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ShowDanceIcons : MonoBehaviour {
    private float dh = 220.3f;
    // Start is called before the first frame update
    public DanceManager dm;
    void Awake()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable() {
        int[] softSequences = new int[6] { 1,5,8,12,18,22}; 
        int[] powerSequences = new int[6] { 0,5,9,12,17,20};
        string ds = dm.danceStar;

        int count = 0;
        for (int i = 0; i < 6; i++) {
            if (ds[i] != 'F')
                count++;
        }

        for (int i = count; i < 6; i++) {
            SoftIcons[i].enabled = false;
            PowerIcons[i].enabled = false;
        }

        int idx = -1;
        for (int i = 0; i < count; i++) {

            idx++;
            if (ds[idx] == 'F') {
                i = i - 1;
                continue;
            }

            SoftIcons[i].sprite = SoftSprites[softSequences[idx]];
            PowerIcons[i].sprite = PowerSprites[powerSequences[idx]];
            SoftIcons[i].SetNativeSize();
            PowerIcons[i].SetNativeSize();
            SoftIcons[i].rectTransform.sizeDelta =
                SoftIcons[i].rectTransform.sizeDelta * dh / SoftIcons[i].rectTransform.rect.height;
            PowerIcons[i].rectTransform.sizeDelta =
                PowerIcons[i].rectTransform.sizeDelta * dh / PowerIcons[i].rectTransform.rect.height;
        }

    }

    public Sprite[] SoftSprites;
    public Sprite[] PowerSprites;
    public Image[] SoftIcons;
    public Image[] PowerIcons;
}
