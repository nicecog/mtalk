using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// 비활성 페이지의 VideoPlayer가 playOnAwake로 메모리를 잡아먹지 않도록 차단.
/// </summary>
public static class MSocialVideoGuard
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void DisarmAfterSceneLoad()
    {
        if (SceneManager.GetActiveScene().name != "MSocial_Bootstrap")
            return;
        MSocialStartupLog.Log("VideoGuard.AfterSceneLoad bootstrap");
        DisarmAll();
    }

    public static void DisarmAll()
    {
        var players = Object.FindObjectsByType<VideoPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var count = 0;
        foreach (var vp in players)
        {
            if (vp == null)
                continue;
            vp.playOnAwake = false;
            vp.Stop();
            count++;
        }
        MSocialStartupLog.Log("VideoGuard stopped " + count + " VideoPlayers");
    }
}
