using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
class MSocialMovieIndexData
{
    public string[] stage1;
    public string[] stage2;
    public string[] intro;
}

/// <summary>
/// StreamingAssets/Movie/movie_index.json — Stage1/2 영상 경로 지연 로드.
/// </summary>
public class MSocialMovieLibrary : MonoBehaviour
{
    public static MSocialMovieLibrary Instance { get; private set; }

    static MSocialMovieIndexData _index;
    static bool _loadStarted;
    static bool _loadDone;
    static bool _usingFallback;

    public static bool IsReady => _loadDone && _index != null;
    public static bool UsingFallback => _usingFallback;

    public static int Stage1Count => _index?.stage1?.Length ?? 0;
    public static int Stage2Count => _index?.stage2?.Length ?? 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureLoaded();
    }

    public static void EnsureLoaded()
    {
        if (_loadDone || _loadStarted)
            return;
        if (Instance == null)
        {
            var go = new GameObject(nameof(MSocialMovieLibrary));
            go.AddComponent<MSocialMovieLibrary>();
            return;
        }
        Instance.StartCoroutine(LoadIndexCoroutine());
    }

    static IEnumerator LoadIndexCoroutine()
    {
        _loadStarted = true;
        var url = Application.streamingAssetsPath + "/Movie/movie_index.json";
        MSocialStartupLog.Log("MovieLibrary loading " + url);

        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 30;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                MSocialStartupLog.Log("MovieLibrary load failed: " + req.error);
                ApplyFallbackIndex("http");
                yield break;
            }

            var text = StripBom(req.downloadHandler.text);
            try
            {
                _index = JsonUtility.FromJson<MSocialMovieIndexData>(text);
                if (_index?.stage1 == null || _index.stage1.Length == 0)
                    throw new Exception("stage1 empty after parse");
                _loadDone = true;
                MSocialStartupLog.Log("MovieLibrary ready stage1=" + Stage1Count + " stage2=" + Stage2Count);
            }
            catch (Exception ex)
            {
                MSocialStartupLog.Log("MovieLibrary parse error: " + ex.Message);
                ApplyFallbackIndex("parse");
            }
        }
    }

    public static IEnumerator WaitUntilReady(float timeoutSeconds = 20f)
    {
        EnsureLoaded();
        float deadline = Time.realtimeSinceStartup + timeoutSeconds;
        while (!_loadDone && Time.realtimeSinceStartup < deadline)
            yield return null;

        if (!_loadDone)
        {
            ApplyFallbackIndex("timeout");
        }
    }

    static void ApplyFallbackIndex(string reason)
    {
        if (_loadDone && _index != null)
            return;

        _index = new MSocialMovieIndexData
        {
            stage1 = BuildStagePaths("Stage1", "s1"),
            stage2 = BuildStagePaths("Stage2", "s2"),
            intro = new[]
            {
                "Animations/폭죽효과.mp4",
                "Movie/Introduction/M-SOCIAL Feel good 트레이닝 소개 영상_v2.mp4",
                "Movie/Introduction/M-SOCIAL 소개_v2.mp4",
                "Movie/Introduction/M-SOCIAL Act good 트레이닝 소개 영상_v2.mp4"
            }
        };
        _loadDone = true;
        _usingFallback = true;
        _loadStarted = false;
        MSocialStartupLog.Log("MovieLibrary fallback (" + reason + ") stage1=" + _index.stage1.Length);
    }

    static string[] BuildStagePaths(string stageFolder, string prefix)
    {
        var paths = new string[32];
        for (int i = 0; i < 32; i++)
        {
            int n = i + 1;
            string bucket = n <= 16 ? "1-16" : "17-32";
            paths[i] = "Movie/Movie/" + stageFolder + "/" + bucket + "/" + prefix + "_" + n.ToString("D3") + ".mp4";
        }
        return paths;
    }

    static string StripBom(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        if (text[0] == '\uFEFF')
            return text.Substring(1);
        return text;
    }

    public static string GetStage1Path(int index)
    {
        if (_index?.stage1 == null || index < 0 || index >= _index.stage1.Length)
            return null;
        return SanitizeIndexPath(_index.stage1[index]);
    }

    public static string GetStage2Path(int index)
    {
        if (_index?.stage2 == null || index < 0 || index >= _index.stage2.Length)
            return null;
        return SanitizeIndexPath(_index.stage2[index]);
    }

    public static string GetIntroPath(int index)
    {
        if (_index?.intro == null || index < 0 || index >= _index.intro.Length)
            return null;
        return SanitizeIndexPath(_index.intro[index]);
    }

    static string SanitizeIndexPath(string path)
    {
        return MSocialStreamingAssetPaths.SanitizeRelativePath(path);
    }
}
