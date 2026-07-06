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
        Image img = GetComponent<Image>();
        int x = idx;
        if (isSong) 
            x += 7;
        img.sprite = dm.danceStar[x] == 'F' ? alpha : star ;
        
    }
}
