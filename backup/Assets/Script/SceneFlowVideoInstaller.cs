using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// Attaches video presenters/players across SceneFlow pages in loaded scenes.
/// </summary>
public static class SceneFlowVideoInstaller
{
    public static void InstallAll()
    {
        SceneFlowVideoPresenter.CacheIntroClipsFromScene();

        var pages = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (var i = 0; i < pages.Length; i++)
        {
            var go = pages[i];
            if (go != null && go.CompareTag("SceneFlow"))
                InstallOnPage(go);
        }

        MBodyDiagLog.Step("VideoInstall", $"Installed on SceneFlow pages={pages.Length}");
    }

    public static void InstallScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        SceneFlowVideoPresenter.CacheIntroClipsFromScene();
        foreach (var root in scene.GetRootGameObjects())
            InstallTree(root);
    }

    static void InstallTree(GameObject go)
    {
        if (go.CompareTag("SceneFlow"))
            InstallOnPage(go);

        for (var i = 0; i < go.transform.childCount; i++)
            InstallTree(go.transform.GetChild(i).gameObject);
    }

    static void InstallOnPage(GameObject page)
    {
        if (page.GetComponent<SceneFlowVideoPresenter>() == null)
            page.AddComponent<SceneFlowVideoPresenter>();

        EnsureUiPlayers(page.transform);
    }

    public static void EnsureUiPlayers(Transform root)
    {
        var players = root.GetComponentsInChildren<VideoPlayer>(true);
        for (var i = 0; i < players.Length; i++)
        {
            var vp = players[i];
            if (vp == null || vp.GetComponent<UiVideoPlayer>() != null)
                continue;

            vp.gameObject.AddComponent<UiVideoPlayer>();
            MBodyDiagLog.Step("VideoInstall", $"UiVideoPlayer added on '{vp.gameObject.name}'");
        }
    }

    public static void RemoveUiPlayers(Transform root)
    {
        var uiPlayers = root.GetComponentsInChildren<UiVideoPlayer>(true);
        for (var i = 0; i < uiPlayers.Length; i++)
        {
            if (uiPlayers[i] != null)
                Object.Destroy(uiPlayers[i]);
        }
    }
}
