using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum MSocialSceneId
{
    Bootstrap,
    Stage1,
    Stage2
}

public static class MSocialSceneRouter
{
    public const string BootstrapScene = "MSocial_Bootstrap";
    public const string Stage1Scene = "MSocial_Stage1";
    public const string Stage2Scene = "MSocial_Stage2";

    static Transform _pageContainer;
    static readonly HashSet<string> Loaded = new HashSet<string>();
    static Coroutine _preloadStage1;

    public static void Initialize(Transform pageContainer)
    {
        _pageContainer = pageContainer;
        MSocialPageRegistry.SetPageContainer(pageContainer);
        MSocialPageRegistry.ScanAndRegister(pageContainer);
        MSocialStartupLog.Log("SceneRouter init container=" + pageContainer.name);
    }

    public static MSocialSceneId SceneForPage(MSocialPageId page)
    {
        switch (page)
        {
            case MSocialPageId.Background:
            case MSocialPageId.IntroLogo:
            case MSocialPageId.Login:
            case MSocialPageId.IntroMovie:
                return MSocialSceneId.Bootstrap;
            case MSocialPageId.Stage1Start:
            case MSocialPageId.Stage1Intro:
            case MSocialPageId.Stage1SelectMap:
            case MSocialPageId.Stage1SelectScene:
            case MSocialPageId.Stage1WatchScene:
            case MSocialPageId.Stage1Positive:
            case MSocialPageId.Stage1Negative:
            case MSocialPageId.Stage1Feedback:
            case MSocialPageId.Stage1End:
                return MSocialSceneId.Stage1;
            default:
                return MSocialSceneId.Stage2;
        }
    }

    static string SceneName(MSocialSceneId id)
    {
        switch (id)
        {
            case MSocialSceneId.Stage1: return Stage1Scene;
            case MSocialSceneId.Stage2: return Stage2Scene;
            default: return BootstrapScene;
        }
    }

    public static bool IsSceneLoaded(MSocialSceneId id) => Loaded.Contains(SceneName(id));

    public static void PreloadStage1(MonoBehaviour host)
    {
        if (host == null || IsSceneLoaded(MSocialSceneId.Stage1) || _preloadStage1 != null)
            return;
        _preloadStage1 = host.StartCoroutine(EnsureLoaded(MSocialSceneId.Stage1));
    }

    public static IEnumerator EnsureLoaded(MSocialSceneId sceneId)
    {
        var name = SceneName(sceneId);
        if (name == BootstrapScene || Loaded.Contains(name))
            yield break;

        MSocialStartupLog.Log("SceneRouter loading " + name);
        var op = SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
        if (op == null)
        {
            MSocialStartupLog.Log("SceneRouter load failed (null op): " + name);
            yield break;
        }
        while (!op.isDone)
            yield return null;

        Loaded.Add(name);
        var scene = SceneManager.GetSceneByName(name);
        if (!scene.IsValid())
            yield break;

        ReparentSceneRoots(scene);
        BindPageMoviePlayers(scene);
        MSocialVideoRtOptimizer.OptimizeSceneIfNeeded(scene);
        MSocialPageRegistry.ScanAndRegister(_pageContainer);
        MSocialStageWireup.WireAll();
        MSocialStartupLog.Log("SceneRouter loaded " + name);
    }

    static void BindPageMoviePlayers(Scene scene)
    {
        if (!scene.IsValid())
            return;

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var marker in root.GetComponentsInChildren<MSocialPageMarker>(true))
            {
                if (marker.gameObject.GetComponent<MSocialPageMoviePlayer>() != null)
                    continue;

                switch (marker.pageId)
                {
                    case MSocialPageId.Stage1Intro:
                        var p1 = marker.gameObject.AddComponent<MSocialPageMoviePlayer>();
                        p1.introIndex = 1;
                        p1.nextPageId = MSocialPageId.Stage1SelectMap;
                        MSocialStartupLog.Log("Bound PageMoviePlayer Stage1Intro");
                        break;
                    case MSocialPageId.Stage2Intro:
                        var p2 = marker.gameObject.AddComponent<MSocialPageMoviePlayer>();
                        p2.introIndex = 3;
                        p2.nextPageId = MSocialPageId.Stage2Select;
                        MSocialStartupLog.Log("Bound PageMoviePlayer Stage2Intro");
                        break;
                }
            }
        }
    }

    public static IEnumerator Unload(MSocialSceneId sceneId)
    {
        var name = SceneName(sceneId);
        if (name == BootstrapScene || !Loaded.Contains(name))
            yield break;

        MSocialStartupLog.Log("SceneRouter unloading " + name);
        var op = SceneManager.UnloadSceneAsync(name);
        if (op != null)
            while (!op.isDone)
                yield return null;
        Loaded.Remove(name);

#if UNITY_ANDROID && !UNITY_EDITOR
        if (MSocialDeviceProfile.IsLowMemoryDevice)
            yield return Resources.UnloadUnusedAssets();
#endif
    }

    static void ReparentSceneRoots(Scene scene)
    {
        if (_pageContainer == null)
            return;

        var parentRt = _pageContainer as RectTransform;
        if (parentRt == null)
            return;

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponent<MSocialSceneContentRoot>() == null)
                continue;

            var markers = root.GetComponentsInChildren<MSocialPageMarker>(true);
            foreach (var marker in markers)
            {
                if (marker.pageId == MSocialPageId.None || marker.pageId == MSocialPageId.Background)
                    continue;
                var rt = marker.GetComponent<RectTransform>();
                if (rt == null)
                    continue;
                rt.SetParent(parentRt, false);
            }

            Object.Destroy(root);
            MSocialStartupLog.Log("SceneRouter flattened pages from " + root.name);
        }
    }

    public static IEnumerator ShowPage(MSocialPageId pageId, MonoBehaviour host = null)
    {
        if (pageId == MSocialPageId.None)
            yield break;

        var needed = SceneForPage(pageId);
        if (needed != MSocialSceneId.Bootstrap)
            yield return EnsureLoaded(needed);

        if (pageId == MSocialPageId.Stage1Start && IsSceneLoaded(MSocialSceneId.Stage2) && host != null)
            yield return UnloadAfterDelay(host, MSocialSceneId.Stage2);

        MSocialPageRegistry.Refresh();
        var go = MSocialPageRegistry.Get(pageId);

        if (go == null)
        {
            MSocialStartupLog.Log("SceneRouter missing page " + pageId);
            yield break;
        }

        EnsureParentChainActive(go);

        MSocialPageRegistry.DeactivateAllExcept(pageId);
        if (pageId != MSocialPageId.Background)
            MSocialPageRegistry.Get(MSocialPageId.Background)?.SetActive(true);
        go.SetActive(true);

        if (pageId == MSocialPageId.Stage1SelectMap)
            MSocialStageWireup.WireAll();

        MSocialStartupLog.Log("SceneRouter show " + pageId);
    }

    public static void ShowPageSync(MSocialPageId pageId)
    {
        if (pageId == MSocialPageId.None)
            return;

        var needed = SceneForPage(pageId);
        if (needed != MSocialSceneId.Bootstrap && !IsSceneLoaded(needed))
        {
            var name = SceneName(needed);
            SceneManager.LoadScene(name, LoadSceneMode.Additive);
            Loaded.Add(name);
            ReparentSceneRoots(SceneManager.GetSceneByName(name));
            MSocialPageRegistry.ScanAndRegister(_pageContainer);
        }

        var go = MSocialPageRegistry.Get(pageId);
        if (go == null)
            MSocialPageRegistry.ScanAndRegister(_pageContainer);
        go = MSocialPageRegistry.Get(pageId);
        if (go == null)
            return;

        MSocialPageRegistry.DeactivateAllExcept(pageId);
        MSocialPageRegistry.Get(MSocialPageId.Background)?.SetActive(true);
        go.SetActive(true);
    }

    public static bool TryResolvePage(GameObject legacyNext, out MSocialPageId pageId)
    {
        pageId = MSocialPageId.None;
        if (legacyNext == null)
            return false;
        var marker = legacyNext.GetComponent<MSocialPageMarker>();
        if (marker != null && marker.pageId != MSocialPageId.None)
        {
            pageId = marker.pageId;
            return true;
        }
        pageId = MSocialPageMarker.FromLegacyName(legacyNext.name);
        return pageId != MSocialPageId.None;
    }

    public static bool IsCrossScene(GameObject from, GameObject to)
    {
        if (from == null || to == null)
            return false;
        return from.scene != to.scene;
    }

    static IEnumerator UnloadAfterDelay(MonoBehaviour host, MSocialSceneId sceneId)
    {
        yield return null;
        yield return host.StartCoroutine(Unload(sceneId));
    }

    static void EnsureParentChainActive(GameObject page)
    {
        if (page == null)
            return;
        var t = page.transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);
            if (t.GetComponent<Canvas>() != null)
                break;
            t = t.parent;
        }
    }

    public static IEnumerator TransitionToStage2(MonoBehaviour host)
    {
        yield return EnsureLoaded(MSocialSceneId.Stage2);
        if (host != null && IsSceneLoaded(MSocialSceneId.Stage1))
            yield return UnloadAfterDelay(host, MSocialSceneId.Stage1);
    }
}
