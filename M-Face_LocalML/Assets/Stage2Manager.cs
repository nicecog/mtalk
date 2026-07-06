using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Stage2Manager : MonoBehaviour {
    public GameObject[] songButtons;

    public int songIdx;
    private void OnEnable() {
        foreach (var item in songButtons) {
            item.gameObject.SetActive(false);
        }
        if (userdata != null && userdata.ID != "admin")
            StartCoroutine(SyncProgressThenRefresh());
        else
            RefreshSongButtons();
    }

    IEnumerator SyncProgressThenRefresh() {
        if (userdata != null)
            yield return userdata.SyncProgressFromServerCoroutine();
        RefreshSongButtons();
    }

    void RefreshSongButtons() {
        if (userdata == null)
            return;

        for(int i=0;i<userdata.playNum;i++)
           songButtons[i].gameObject.SetActive(true);

        for (int i = 0; i < userdata.playNum; i++) {
            //if(userdata.ItemString[i*2]=='T'&&userdata.ItemString[i*2 + 1]=='T')
                //songButtons[i].gameObject.SetActive(false);
        }
            
        if (userdata.playNum > 1) {
            tutorial.SetActive(false);
        }
    }

    public GameObject tutorial;
    public Login userdata;

    public void selectSong(int idx) {
        songIdx = idx;
        ItemSelect.SetActive(true);
    }
    

    public GameObject ItemSelect;
}
