using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LV4PlayGame : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake() {
        auso = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable() {
        StartCoroutine(StartGame());
    }

    public AudioClip[] bgms;
    public void setSong() {
        int sp = dm.level == 1 ? 0 : dm.level == 3 ? 2 : 1;
        auso.clip = bgms[dm.song_idx * 3 + sp];
    }

    private AudioSource auso;
    public DanceManager dm;
    public VideoCaptureCam vcc;
    public GameObject nextButton;

    IEnumerator StartGame() {
        nextButton.SetActive(false);
        setSong();
        vcc.prefix = "DanceKing";
        vcc.startRecord();
        yield return new WaitForSecondsRealtime(3f);
        
        auso.Play();
        while (auso.isPlaying) {
            yield return null;
            
        }
        vcc.stopRecording(true);
        dm.fileManager.WriteResult("Dance,4,Song:"+(dm.song_idx+1)+",N/A");

        int idx = dm.song_idx;
        string temp = dm.danceStar;
        char[] tx = new char[7];
        for (int i = 7; i < 14; i++) {
            tx[i-7] = temp[i];
        } 
        tx[idx] = 'K';
        dm.danceStar = String.Concat(temp[0],temp[1],temp[2],temp[3],temp[4],temp[5],temp[6],
            tx[0],tx[1],tx[2],tx[3],tx[4],tx[5],tx[6]);
        dm.fileManager.danceStar = dm.danceStar;
        dm.fileManager.UpdateResult();
        nextButton.SetActive(true);
    }
}
