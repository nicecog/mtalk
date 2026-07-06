using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class Stage1Dance : MonoBehaviour
{
    public VideoPlayer vp;
    public string[] vcs;

    AudioSource asc;
    RawImage _videoImage;

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

    IEnumerator WaitForVideoEnd() {
        asc.Play();
        yield return new WaitForSeconds(1f);
        while (asc.isPlaying)
            yield return null;
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

    public Button nextButton;

    public void ClickNextButton() {
        gameObject.SetActive(false);
        s1m.ProgressDone();
    }

    public Stage1Manager s1m;
}
