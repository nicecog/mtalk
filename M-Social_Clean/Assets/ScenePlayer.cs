using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class ScenePlayer : MonoBehaviour
{
    public RawImage ri;
    public int SceneIndex;
    public EmotionSelect es;
    public SceneSelect sceneselect;
    public int[] VideoCodes;
    public int[] SceneEmotions;
    public int[] prv;
    public VideoPlayer vp;
    public string[] titles;

    Coroutine _playRoutine;
    string _currentPath;
    AudioController _promptAudio;

    void OnEnable()
    {
        if (_playRoutine != null)
            StopCoroutine(_playRoutine);
        _playRoutine = StartCoroutine(PlaySceneRoutine());
    }

    IEnumerator PlaySceneRoutine()
    {
        if (sceneselect == null || vp == null || VideoCodes == null || VideoCodes.Length == 0)
        {
            MSocialStartupLog.Log("ScenePlayer missing refs sceneselect=" + (sceneselect != null)
                + " vp=" + (vp != null));
            yield break;
        }

        if (sceneselect.s1m == null || sceneselect.s1m.userdata == null)
        {
            MSocialStageWireup.WireAll();
            if (sceneselect.s1m == null || sceneselect.s1m.userdata == null)
            {
                MSocialStartupLog.Log("ScenePlayer missing userdata");
                yield break;
            }
        }

        if (ri == null)
            ri = MSocialVideoPlayerHelper.FindLinkedRawImage(vp);
        if (ri != null)
        {
            ri.enabled = true;
            ri.color = Color.black;
        }

        _promptAudio = GetComponent<AudioController>();
        if (_promptAudio != null)
            _promptAudio.enabled = true;

        yield return MSocialMovieLibrary.WaitUntilReady();

        int code = sceneselect.sceneCategory * 100 + (sceneselect.sceneNumber + 2 * sceneselect.nowPage);
        if (es != null)
            es.gameObject.SetActive(false);

        SceneIndex = -1;
        for (int i = 0; i < VideoCodes.Length; i++)
        {
            if (VideoCodes[i] == code)
                SceneIndex = i;
        }

        if (SceneIndex < 0)
        {
            MSocialStartupLog.Log("ScenePlayer unknown code=" + code + " cat=" + sceneselect.sceneCategory
                + " num=" + sceneselect.sceneNumber + " page=" + sceneselect.nowPage);
            yield break;
        }

        sceneselect.s1m.userdata.todayCode.Add(code);

        _currentPath = MSocialMovieLibrary.GetStage1Path(SceneIndex);
        if (string.IsNullOrEmpty(_currentPath))
        {
            MSocialStartupLog.Log("ScenePlayer missing path index=" + SceneIndex + " code=" + code);
            yield break;
        }

        MSocialStartupLog.Log("ScenePlayer code=" + code + " index=" + SceneIndex + " path=" + _currentPath);

        var prepareTask = StartCoroutine(MSocialVideoPlayerHelper.PrepareUrl(vp, _currentPath, ri));
        yield return prepareTask;

        if (!MSocialVideoPlayerHelper.IsPrepared(vp))
        {
            MSocialStartupLog.Log("ScenePlayer could not prepare " + _currentPath);
            yield break;
        }

        yield return MSocialVideoPlayerHelper.PlayPrepared(vp);

        yield return new WaitForSecondsRealtime(0.2f);
        if (es != null)
            es.gameObject.SetActive(true);

        _playRoutine = null;
    }

    void OnDisable()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }
        MSocialVideoPlayerHelper.ReleaseTarget(vp, ri);
        _currentPath = null;
    }
}
