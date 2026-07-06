using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Lightweight login shell: after bootstrap benchmark, loads main MBody scene.
/// Future: keep login UI here and load Body/Dance packs additively only.
/// </summary>
public sealed class LoginSceneController : MonoBehaviour
{
    const string MainSceneName = "MBody";

    IEnumerator Start()
    {
        if (SceneManager.GetSceneByName(MainSceneName).isLoaded)
            yield break;

        var op = SceneManager.LoadSceneAsync(MainSceneName, LoadSceneMode.Single);
        while (op != null && !op.isDone)
            yield return null;
    }
}
