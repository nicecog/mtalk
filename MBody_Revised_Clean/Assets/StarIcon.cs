using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StarIcon : MonoBehaviour
{
    public bool isSong = false;
    public int idx;

    public Sprite alpha, star;
    public DanceManager dm;
    private void OnEnable() {
        Refresh();
    }

    public void Refresh() {
        if (dm == null)
            dm = FindFirstObjectByType<DanceManager>(FindObjectsInactive.Include);

        var img = GetComponent<Image>();
        if (img == null || dm == null || string.IsNullOrEmpty(dm.danceStar))
            return;

        var x = idx;
        if (isSong)
            x += 7;
        if (x < 0 || x >= dm.danceStar.Length)
            return;

        img.sprite = dm.danceStar[x] == 'F' ? alpha : star;
    }
}
