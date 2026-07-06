using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loads optional additive scene packs (Body/Dance) before navigation.
/// </summary>
public static class ScenePackLoader
{
    static readonly HashSet<string> Loaded = new HashSet<string>();

    public static bool IsLoaded(string sceneName) => Loaded.Contains(sceneName);

    public static IEnumerator EnsureLoaded(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || Loaded.Contains(sceneName))
        {
            MBodyDiagLog.Step("Pack", $"EnsureLoaded skip '{sceneName}' loaded={Loaded.Contains(sceneName)}");
            yield break;
        }

        MBodyDiagLog.Step("Pack", $"Loading additive scene '{sceneName}'");

        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op != null)
            {
                while (!op.isDone)
                    yield return null;
            }
        }
        else
        {
            MBodyDiagLog.Error("Pack", $"Scene '{sceneName}' is not in Build Settings / cannot stream");
        }

        Loaded.Add(sceneName);
        var scene = SceneManager.GetSceneByName(sceneName);
        if (scene.IsValid() && scene.isLoaded)
        {
            ScenePackUiBinder.AttachPackUi(scene);
            SceneFlowRegistry.RegisterScene(scene);
            SceneFlowVideoInstaller.InstallScene(scene);
            MBodyDiagLog.Step("Pack", $"Loaded '{sceneName}' roots={scene.GetRootGameObjects().Length}");
        }

        if (sceneName == "MBodyBodyPack")
        {
            var body = Object.FindFirstObjectByType<BodyManager>(FindObjectsInactive.Include);
            if (body != null)
                body.ResolvePackRefs();
        }
        else if (sceneName == "MBodyDancePack")
        {
            var dance = Object.FindFirstObjectByType<DanceManager>(FindObjectsInactive.Include);
            if (dance != null)
                dance.ResolvePackRefs();
        }
    }
}
