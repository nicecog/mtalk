using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
public class Stage1Sing : MonoBehaviour
{
    public VideoPlayer vp;

    public string[] vcs;
    private AudioSource asc;

    public AudioClip[] sources;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnEnable() {
        asc = GetComponent<AudioSource>();
        asc.loop = false;
        asc.playOnAwake = false;
        if (vcs.Length > 0) {
            vp.source = VideoSource.Url;
            vp.url = Application.streamingAssetsPath + "/" + vcs[UnityEngine.Random.Range(0, vcs.Length)];
            //vp.Play();
        }

        nextButton.gameObject.SetActive(false);
        StartCoroutine(WaitForVideoEnd());
    }

    IEnumerator WaitForVideoEnd() {
        asc.clip = sources[0];
        asc.Play();
        yield return new WaitForSeconds(1f);
        while (asc.isPlaying)
            yield return null;
        asc.clip = sources[1];
        asc.Play();
        yield return new WaitForSeconds(1f);
        while (asc.isPlaying)
            yield return null;
        asc.clip = sources[2];
        asc.Play();
        yield return new WaitForSeconds(1f);
        while (asc.isPlaying)
            yield return null;
        asc.clip = sources[3];
        
        yield return new WaitForSeconds(1f);
        asc.Play();
        yield return new WaitForSeconds(1f);
        while (asc.isPlaying)
            yield return null;

        //do something
        nextButton.gameObject.SetActive(true);
    }

    public Button nextButton;
    // Update is called once per frame
    void Update()
    {
        
    }

    public void ClickNextButton() {
        gameObject.SetActive(false);
        s1m.ProgressDone();
    }

    public Stage1Manager s1m;
}
