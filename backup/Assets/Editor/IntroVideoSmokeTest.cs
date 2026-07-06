#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// Editor smoke test: verify post-login intro clip loads and prepares on PC.
/// Menu: MBody/Diagnostics/Run Intro Video Smoke Test
/// </summary>
public static class IntroVideoSmokeTest
{
    const string PostLoginClipGuid = "8a13191df834d974ea3a8063394bb830";

    [MenuItem("MBody/Diagnostics/Run Intro Video Smoke Test")]
    public static void RunFromMenu()
    {
        EditorApplication.update += RunOnce;
    }

    public static void RunBatch()
    {
        MBodyDiagLog.Step("SmokeTest", "Batch intro video smoke test starting");

        var clipPath = AssetDatabase.GUIDToAssetPath(PostLoginClipGuid);
        MBodyDiagLog.Step("SmokeTest", $"Clip asset path: {clipPath ?? "(missing)"}");

        var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(clipPath);
        if (clip == null)
        {
            MBodyDiagLog.Error("SmokeTest", "Post-login VideoClip failed to load by GUID");
            EditorApplication.Exit(1);
            return;
        }

        MBodyDiagLog.Step("SmokeTest", $"PASS clip name='{clip.name}' size={clip.width}x{clip.height} length={clip.length:F2}s");
        MBodyDiagLog.Step("SmokeTest", "Korean filename is OK on PC if clip loads by GUID");
        EditorApplication.Exit(0);
    }

    static bool started;

    static void RunOnce()
    {
        if (started)
            return;

        started = true;
        EditorApplication.update -= RunOnce;
        EditorCoroutine.Start(RunCoroutine());
    }

    static IEnumerator RunCoroutine()
    {
        MBodyDiagLog.Step("SmokeTest", "Starting intro video smoke test on PC");

        var clipPath = AssetDatabase.GUIDToAssetPath(PostLoginClipGuid);
        MBodyDiagLog.Step("SmokeTest", $"Clip asset path: {clipPath ?? "(missing)"}");

        var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(clipPath);
        if (clip == null)
        {
            MBodyDiagLog.Error("SmokeTest", "Post-login VideoClip failed to load by GUID");
            yield break;
        }

        MBodyDiagLog.Step("SmokeTest", $"Clip name='{clip.name}' size={clip.width}x{clip.height} length={clip.length:F2}s");

        var go = new GameObject("SmokeTestVideo");
        var image = go.AddComponent<UnityEngine.UI.Image>();
        var vp = go.AddComponent<VideoPlayer>();
        go.AddComponent<UiVideoPlayer>();

        vp.source = VideoSource.VideoClip;
        vp.clip = clip;
        vp.playOnAwake = false;

        vp.Prepare();
        var timeout = 10f;
        while (!vp.isPrepared && timeout > 0f)
        {
            timeout -= 0.1f;
            yield return null;
        }

        if (!vp.isPrepared)
        {
            MBodyDiagLog.Error("SmokeTest", $"Prepare timed out for clip '{clip.name}'");
            Object.DestroyImmediate(go);
            yield break;
        }

        vp.Play();
        MBodyDiagLog.Step("SmokeTest", $"Play started. isPlaying={vp.isPlaying} frame={vp.frame}");

        yield return new WaitForSeconds(1f);
        MBodyDiagLog.Step("SmokeTest", $"After 1s: isPlaying={vp.isPlaying} frame={vp.frame} texture={(vp.texture != null)}");

        Object.DestroyImmediate(go);
        MBodyDiagLog.Step("SmokeTest", "PASS on PC - clip loads and prepares");

        if (!Application.isPlaying)
            LoadSceneIntroPlayers();
    }

    static void LoadSceneIntroPlayers()
    {
        var scene = SceneManager.GetSceneByName("MBody");
        if (!scene.isLoaded)
        {
            MBodyDiagLog.Warn("SmokeTest", "MBody scene not loaded; open MBody.unity and press Play for scene wiring check");
            return;
        }

        var players = Object.FindObjectsByType<VideoPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var count = 0;
        for (var i = 0; i < players.Length; i++)
        {
            var vp = players[i];
            if (vp.clip == null)
                continue;

            var page = FindSceneFlowAncestor(vp.transform);
            MBodyDiagLog.Step("SmokeTest", $"Scene VP #{count++}: page='{page}' go='{vp.gameObject.name}' clip='{vp.clip.name}' guid path='{AssetDatabase.GetAssetPath(vp.clip)}'");
        }
    }

    static string FindSceneFlowAncestor(Transform t)
    {
        while (t != null)
        {
            if (t.CompareTag("SceneFlow"))
                return $"{t.name} (id={t.gameObject.GetInstanceID()})";
            t = t.parent;
        }

        return "(none)";
    }
}

/// <summary>Minimal coroutine runner for Editor batch checks.</summary>
public static class EditorCoroutine
{
    public static void Start(IEnumerator routine)
    {
        EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            try
            {
                if (routine == null || !routine.MoveNext())
                    EditorApplication.update -= tick;
            }
            catch (System.Exception ex)
            {
                MBodyDiagLog.Error("SmokeTest", ex.ToString());
                EditorApplication.update -= tick;
            }
        };
        EditorApplication.update += tick;
    }
}
#endif
