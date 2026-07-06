using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class Stage2ScenePlay : MonoBehaviour
{
    public RawImage img;
    public ScenePlayer sp;
    public Stage2Manager s2m;
    public GameObject es;
    public VideoPlayer vp;

    AudioSource asc;
    Coroutine _routine;
    string _currentPath;

    void OnEnable()
    {
        if (_routine != null)
            StopCoroutine(_routine);
        _routine = StartCoroutine(PlayRoutine());
    }

    IEnumerator PlayRoutine()
    {
        asc = GetComponent<AudioSource>();
        if (asc != null)
        {
            asc.loop = false;
            asc.playOnAwake = false;
            asc.Stop();
        }

        if (s2m == null || vp == null)
            yield break;

        yield return MSocialMovieLibrary.WaitUntilReady();

        if (es != null)
            es.gameObject.SetActive(false);

        _currentPath = MSocialMovieLibrary.GetStage2Path(s2m.scenesToDo);
        if (string.IsNullOrEmpty(_currentPath))
        {
            MSocialStartupLog.Log("Stage2ScenePlay missing path index=" + s2m.scenesToDo);
            yield break;
        }

        MSocialStartupLog.Log("Stage2ScenePlay path=" + _currentPath);

        if (asc != null)
            asc.Play();
        yield return new WaitForSecondsRealtime(6.5f);

        yield return MSocialVideoPlayerHelper.PrepareUrl(vp, _currentPath, img);
        if (MSocialVideoPlayerHelper.IsPrepared(vp))
            yield return MSocialVideoPlayerHelper.PlayPrepared(vp);
        else
            MSocialStartupLog.Log("Stage2ScenePlay could not prepare " + _currentPath);

        yield return new WaitForSecondsRealtime(1f);
        if (es != null)
            es.gameObject.SetActive(true);

        _routine = null;
    }

    void OnDisable()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
        MSocialVideoPlayerHelper.ReleaseTarget(vp, img);
        _currentPath = null;
    }
}
