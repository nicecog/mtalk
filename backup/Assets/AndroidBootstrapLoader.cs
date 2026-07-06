using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class AndroidBootstrapLoader : MonoBehaviour
{
    const string MainSceneName = "MBody";
    const string LoginSceneName = "Login";

    IEnumerator Start()
    {
        if (SceneManager.GetActiveScene().name == MainSceneName ||
            SceneManager.GetActiveScene().name == LoginSceneName)
            yield break;

        var performance = PerformanceManager.EnsureExists();
        yield return performance.InitializeAsync();

        var nextScene = Application.CanStreamedLevelBeLoaded(LoginSceneName)
            ? LoginSceneName
            : MainSceneName;
        SceneManager.LoadScene(nextScene, LoadSceneMode.Single);
    }
}
