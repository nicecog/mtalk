using System.Collections;
using UnityEngine;

/// <summary>
/// Pulls latest dayCount/progressString from server on key page entry.
/// </summary>
public class MSocialProgressSync : MonoBehaviour
{
    static readonly string[] SyncPageNames = {
        "SceneSelect",
        "Stage1Manager",
        "ActSelect",
        "Stage1SelectMap"
    };

    bool syncInFlight;

    void OnEnable()
    {
        if (!ShouldPullServerProgress())
            return;

        StartCoroutine(PullServerProgress());
    }

    bool ShouldPullServerProgress()
    {
        foreach (var pageName in SyncPageNames) {
            if (name == pageName || name.StartsWith(pageName))
                return true;
        }
        return false;
    }

    IEnumerator PullServerProgress()
    {
        if (syncInFlight)
            yield break;

        syncInFlight = true;
        try {
            var login = FindFirstObjectByType<Login>(FindObjectsInactive.Include);
            if (login == null || login.ID == "admin" || login.ID == "Test") {
                yield break;
            }

            var api = MSocialApiClient.Instance;
            if (api == null || !api.HasSession) {
                Debug.LogWarning("[MSocialProgressSync] " + name + ": no session; using local state");
                yield break;
            }

            var ok = false;
            yield return login.SyncProgressFromServerCoroutine(success => ok = success);

            if (ok)
                Debug.Log("[MSocialProgressSync] " + name + ": server progress applied");
            else
                Debug.LogWarning("[MSocialProgressSync] " + name + ": server sync failed; using local state");
        } finally {
            syncInFlight = false;
        }
    }
}
