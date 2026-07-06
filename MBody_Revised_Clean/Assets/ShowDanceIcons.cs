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
        Refresh();
    }

    public void Refresh() {
        if (dm == null || SoftIcons == null || PowerIcons == null)
            return;

        string ds = dm.danceStar;
        if (string.IsNullOrEmpty(ds) || ds.Length < 6)
            ds = "FFFFFF-FFFFFFF";

        int[] softSequences = new int[6] { 1, 5, 8, 12, 18, 22 };
        int[] powerSequences = new int[6] { 0, 5, 9, 12, 17, 20 };

        int count = 0;
        for (int i = 0; i < 6; i++) {
            if (ds[i] != 'F')
                count++;
        }

        for (int i = 0; i < SoftIcons.Length && i < PowerIcons.Length; i++) {
            SoftIcons[i].enabled = i < count;
            PowerIcons[i].enabled = i < count;
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

    void OnDisable() {
        if (SoftIcons == null)
            return;

        for (int i = 0; i < SoftIcons.Length; i++) {
            if (SoftIcons[i] != null)
                SoftIcons[i].enabled = false;
            if (PowerIcons != null && i < PowerIcons.Length && PowerIcons[i] != null)
                PowerIcons[i].enabled = false;
        }
    }

    public Sprite[] SoftSprites;
    public Sprite[] PowerSprites;
    public Image[] SoftIcons;
    public Image[] PowerIcons;
}
