using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class Stage1Count : MonoBehaviour
{
    public VideoPlayer vp;
    public AudioClip ac;
    public string[] vcs;
    AudioSource asc;
    RawImage _videoImage;

    public bool buttonPressed;

    void OnDisable() {
        MSocialVideoPlayerHelper.ReleaseFeedbackClip(vp, _videoImage);
    }

    void OnEnable() {
        asc = GetComponent<AudioSource>();
        asc.loop = false;
        asc.playOnAwake = false;
        _videoImage = MSocialVideoPlayerHelper.FindLinkedRawImage(vp);
        nextButton.gameObject.SetActive(false);
        StartCoroutine(WaitForVideoEnd());
    }

    public Button startMovie;

    IEnumerator WaitForVideoEnd() {
        if (name == "PlayInstrument")
            startMovie.gameObject.SetActive(true);
        asc.Play();
        yield return new WaitForSeconds(1f);
        while (asc.isPlaying)
            yield return new WaitForSeconds(0.2f);
        buttonPressed = false;

        if (name == "PlayInstrument") {
            while (!buttonPressed)
                yield return null;
            startMovie.gameObject.SetActive(false);
        }

        if (vcs.Length > 0) {
            var clip = vcs[Random.Range(0, vcs.Length)];
            yield return MSocialVideoPlayerHelper.PrepareFeedbackClip(vp, clip, _videoImage);
            if (MSocialVideoPlayerHelper.IsPrepared(vp))
            {
                vp.Play();
                if (name != "PlayInstrument" && ac != null)
                    asc.PlayOneShot(ac);
                yield return new WaitForSeconds(1f);
                while (vp.isPlaying)
                    yield return new WaitForSeconds(0.2f);
            }
        }
        nextButton.gameObject.SetActive(true);
    }

    // Keep explicit method for serialized UnityEvent binding in scene.
    public void set_buttonPressed(bool value) {
        buttonPressed = value;
    }

    public Button nextButton;

    public void ClickNextButton() {
        gameObject.SetActive(false);
        s1m.ProgressDone();
    }

    public Stage1Manager s1m;
}

