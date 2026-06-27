using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LeveledMask : MonoBehaviour
{
    private void Awake() {
        dm = FindFirstObjectByType<DanceManager>();
    }
    public DanceManager dm;
    public GameObject[] masks;
    private void OnEnable() {
        dm.fileManager.UpdateResult();
        if (name == "MusicSelect") {
            foreach(var v in masks)
                v.SetActive(dm.playTimes < 5);
            
        }
        
        else if (name == "DanceSelect"||name=="Result") {
            masks[0].SetActive(false);
            masks[3].SetActive(false);
            masks[1].SetActive(!(dm.playTimes>2));
            masks[4].SetActive(!(dm.playTimes>3));
            masks[2].SetActive(!(dm.playTimes>4));
            masks[5].SetActive(!(dm.playTimes>4));

        }
        
        else if (name == "DanceLevelSelect") {
            if (dm.danceNum == 1) {
                masks[0].SetActive(false);
                masks[1].SetActive(false);
                masks[2].SetActive(dm.playTimes<2);
            } else if (dm.danceNum == 2) {
                masks[0].SetActive(dm.playTimes<3);
                masks[1].SetActive(dm.playTimes<3);
                masks[2].SetActive(dm.playTimes<4);
            }else if (dm.danceNum == 3) {
                masks[0].SetActive(dm.playTimes<5);
                masks[1].SetActive(dm.playTimes<5);
                masks[2].SetActive(dm.playTimes<7);
            }else if (dm.danceNum == 4) {
                masks[0].SetActive(false);
                masks[1].SetActive(false);
                masks[2].SetActive(dm.playTimes<3);
            }else if (dm.danceNum == 5) {
                masks[0].SetActive(dm.playTimes<4);
                masks[1].SetActive(dm.playTimes<4);
                masks[2].SetActive(dm.playTimes<5);
            }else if (dm.danceNum == 6) {
                masks[0].SetActive(dm.playTimes<5);
                masks[1].SetActive(dm.playTimes<6);
                masks[2].SetActive(dm.playTimes<8);
            }
            masks[3].SetActive(dm.danceStar[dm.danceNum-1]=='F');
            
        }
    }
}
