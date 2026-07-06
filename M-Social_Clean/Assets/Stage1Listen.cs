using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class Stage1Listen : MonoBehaviour
{
    public VideoPlayer vp;
    public string[] vcs;
    AudioSource asc;
    RawImage _videoImage;

    public AudioClip[] sources;
    public Image guard;
    public bool pressbutton;

    void OnDisable() {
        MSocialVideoPlayerHelper.ReleaseFeedbackClip(vp, _videoImage);
    }

    void OnEnable() {
        asc = GetComponent<AudioSource>();
        asc.loop = false;
        asc.playOnAwake = false;
        _videoImage = MSocialVideoPlayerHelper.FindLinkedRawImage(vp);
        guard.enabled = true;
        nextButton.gameObject.SetActive(false);
        StartCoroutine(WaitForVideoEnd());
    }

    IEnumerator WaitForVideoEnd() {
        asc.clip = sources[0];
        asc.Play();
        pressbutton = false;
        yield return new WaitForSeconds(1f);
        while (asc.isPlaying)
            yield return new WaitForEndOfFrame();
        pressbutton = false;
        while (!pressbutton)
            yield return new WaitForEndOfFrame();
        asc.clip = sources[Random.Range(1, 4)];
        guard.enabled = false;
        asc.Play();
        yield return new WaitForSeconds(1f);
        while (asc.isPlaying)
            yield return new WaitForEndOfFrame();
        if (vcs.Length > 0) {
            var clip = vcs[Random.Range(0, vcs.Length)];
            yield return MSocialVideoPlayerHelper.PrepareFeedbackClip(vp, clip, _videoImage);
            if (MSocialVideoPlayerHelper.IsPrepared(vp))
            {
                vp.Play();
                yield return new WaitForSeconds(1f);
                while (vp.isPlaying)
                    yield return null;
            }
        }
        nextButton.gameObject.SetActive(true);
    }

    // Keep explicit method for serialized UnityEvent binding in scene.
    public void set_pressbutton(bool value) {
        pressbutton = value;
    }

    public Button nextButton;

    public void ClickNextButton() {
        gameObject.SetActive(false);
        s1m.ProgressDone();
    }

    public Stage1Manager s1m;
}

