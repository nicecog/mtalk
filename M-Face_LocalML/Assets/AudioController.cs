using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

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
    }

    private IEnumerator sequentialClip() {
        yield return new WaitForSeconds(1f);
        for (int i = 0; i < clips.Length; i++) {
            asc.clip = clips[i];
            asc.Play();
            while (asc.isPlaying)
                yield return null;
            
            yield return new WaitForSeconds(1f);
        }
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
        if (isSM1) {
            Stage1Manager s1m = FindObjectOfType<Stage1Manager>();
            s1m.ic.sub.enabled = true;
            s1m.ic.sub.sprite = s1m.ic.subSprites[5];
            s1m.ic.sub.SetNativeSize();
            Debug.Log("HI");
        }

        voiceOn = false;
    }

    public bool isSM1 = false;
    private string toplayString;
    public void playClip(string ac) {
        toplayString = ac;
        StartCoroutine(ClipPlayingSequence());
    }
    IEnumerator ClipPlayingSequence2() {
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
        asc.PlayOneShot(toPlay);
        yield return new WaitForSeconds(7f);
        
        if (isSM1) {
            Stage1Manager s1m = FindObjectOfType<Stage1Manager>();
            s1m.ic.sub.enabled = true;
            s1m.ic.sub.sprite = s1m.ic.subSprites[5];
            s1m.ic.sub.SetNativeSize();
            Debug.Log("HI2");
        }
        voiceOn = false;
    }
    public void playClip2(string ac) {
        toplayString = ac;
        StartCoroutine(ClipPlayingSequence2());
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
