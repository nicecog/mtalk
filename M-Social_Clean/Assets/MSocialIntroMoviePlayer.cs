using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// IntroMovie 페이지 — StreamingAssets URL 재생. 종료·실패 시 Stage1Start로 이동.
/// </summary>
public class MSocialIntroMoviePlayer : MonoBehaviour
{
    public int introIndex = 2;
    public VideoPlayer videoPlayer;
    public RawImage rawImage;

    ButtonFlow _flow;
    bool _navigated;

    void Awake()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();
        if (rawImage == null)
            rawImage = GetComponent<RawImage>();
        _flow = GetComponent<ButtonFlow>();
        if (videoPlayer != null)
            videoPlayer.playOnAwake = false;
    }

    void OnEnable()
    {
        _navigated = false;
        MSocialMovieLibrary.EnsureLoaded();
        StartCoroutine(PlayIntro());
    }

    IEnumerator PlayIntro()
    {
        if (videoPlayer == null)
        {
            AdvanceToStage1();
            yield break;
        }

        if (rawImage != null)
            rawImage.color = Color.black;

        yield return MSocialMovieLibrary.WaitUntilReady();
        var path = MSocialMovieLibrary.GetIntroPath(introIndex);
        if (string.IsNullOrEmpty(path))
        {
            MSocialStartupLog.Log("IntroMovie missing path index=" + introIndex);
            AdvanceToStage1();
            yield break;
        }

        yield return MSocialVideoPlayerHelper.PrepareUrl(videoPlayer, path, rawImage);
        if (MSocialVideoPlayerHelper.IsPrepared(videoPlayer))
        {
            if (rawImage != null)
                rawImage.color = Color.white;
            yield return MSocialVideoPlayerHelper.PlayPrepared(videoPlayer);
        }
        else
            MSocialStartupLog.Log("IntroMovie prepare failed: " + path);

        AdvanceToStage1();
    }

    void AdvanceToStage1()
    {
        if (_navigated)
            return;
        _navigated = true;
        MSocialStartupLog.Log("IntroMovie -> Stage1Start");

        var flow = MSocialAppServices.Instance != null ? MSocialAppServices.Instance.Flow : null;
        if (flow != null)
            flow.GoToPage(MSocialPageId.Stage1Start);
        else if (_flow != null)
            StartCoroutine(MSocialSceneRouter.ShowPage(MSocialPageId.Stage1Start, _flow));
    }

    void OnDisable()
    {
        MSocialVideoPlayerHelper.ReleaseTarget(videoPlayer, rawImage);
    }
}
