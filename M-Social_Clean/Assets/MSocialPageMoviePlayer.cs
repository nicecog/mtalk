using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Stage1Intro 등 — StreamingAssets URL 재생. 종료·실패 시 다음 페이지로 이동.
/// </summary>
public class MSocialPageMoviePlayer : MonoBehaviour
{
    public int introIndex = 1;
    public MSocialPageId nextPageId = MSocialPageId.Stage1SelectMap;
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
        StartCoroutine(Play());
    }

    IEnumerator Play()
    {
        if (videoPlayer == null)
        {
            AdvanceNext();
            yield break;
        }

        if (rawImage != null)
            rawImage.color = Color.black;

        yield return MSocialMovieLibrary.WaitUntilReady();
        var path = MSocialMovieLibrary.GetIntroPath(introIndex);
        if (string.IsNullOrEmpty(path))
        {
            MSocialStartupLog.Log("PageMovie missing path index=" + introIndex);
            AdvanceNext();
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
            MSocialStartupLog.Log("PageMovie prepare failed: " + path);

        AdvanceNext();
    }

    void AdvanceNext()
    {
        if (_navigated)
            return;
        _navigated = true;
        MSocialStartupLog.Log("PageMovie done " + name + " -> " + nextPageId);

        var flow = MSocialAppServices.Instance != null ? MSocialAppServices.Instance.Flow : null;
        if (flow != null)
            flow.GoToPage(nextPageId);
        else if (_flow != null)
            StartCoroutine(MSocialSceneRouter.ShowPage(nextPageId, _flow));
    }

    void OnDisable()
    {
        MSocialVideoPlayerHelper.ReleaseTarget(videoPlayer, rawImage);
    }
}
