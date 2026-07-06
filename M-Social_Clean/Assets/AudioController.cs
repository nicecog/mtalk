using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AudioController : MonoBehaviour {
    public bool isSequential;
    public AudioSource asc;

    public AudioClip[] clips;
    void OnEnable() {
        asc = GetComponent<AudioSource>();
        asc.playOnAwake = false;
        asc.loop = false;
        if (isSequential)
            StartCoroutine(sequentialClip());
        nowPage = 1;
        if(nextButton!=null)
            nextButton.gameObject.SetActive(false);
    }

    private IEnumerator sequentialClip() {
        voiceOn = true;
        for (int i = 0; i < clips.Length; i++) {
            
            yield return new WaitForSeconds(1f);
            asc.clip = clips[i];
            asc.Play();
            while (asc.isPlaying)
                yield return null;
            
        }

        voiceOn = false;
        if(nextButton!=null)
            nextButton.gameObject.SetActive(true);
    }

    public Button nextButton;
    public Button nextPage;
    public int nowPage;
    public void ViewNext() {
        
    }
    public void playNumber(int x) {
        asc.clip = clips[x];
        asc.Play();
        voiceOn = true;
        StartCoroutine(instructionVoice());
    }

    IEnumerator instructionVoice() {
        while (asc.isPlaying)
            yield return null;
        voiceOn = false;
    }

    public bool voiceOn = false;
    private AudioClip toPlay;
    IEnumerator ClipPlayingSequence() {
        voiceOn = true;
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(
            Application.streamingAssetsPath + "/" + toplayString, AudioType.MPEG)) {
            yield return www.SendWebRequest();
            if(www.result == UnityWebRequest.Result.ConnectionError)
                Debug.Log(www.error);
            else {
                toPlay = DownloadHandlerAudioClip.GetContent(www);
            }
        }
        asc.Stop();
        asc.clip = clips[0];
        asc.Play();
        while (asc.isPlaying)
            yield return null;
        yield return new WaitForSeconds(1f);
        
        asc.PlayOneShot(toPlay);
        yield return new WaitForSeconds(7f);
        voiceOn = false;
    }

    private string toplayString;
    public void playClip(string ac) {
        toplayString = ac;
        StartCoroutine(ClipPlayingSequence());
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
