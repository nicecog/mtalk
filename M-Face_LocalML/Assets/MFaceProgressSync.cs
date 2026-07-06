using System.Collections;
using UnityEngine;

/// <summary>
/// Pulls latest dayCount/itemString from server on key page entry (MBody LeveledMask pattern).
/// </summary>
public class MFaceProgressSync : MonoBehaviour
{
    static readonly string[] SyncPageNames = {
        "Stage2Manager",
        "ItemSelect",
        "InfoWindow",
        "Stage1Start",
        "Stage1Manager"
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
            if (login == null || login.ID == "admin") {
                yield break;
            }

            var api = MFaceApiClient.Instance;
            if (api == null || !api.HasSession) {
                Debug.LogWarning("[MFaceProgressSync] " + name + ": no session; using local state");
                yield break;
            }

            var ok = false;
            yield return login.SyncProgressFromServerCoroutine(success => ok = success);

            if (ok)
                Debug.Log("[MFaceProgressSync] " + name + ": server progress applied");
            else
                Debug.LogWarning("[MFaceProgressSync] " + name + ": server sync failed; using local state");
        } finally {
            syncInFlight = false;
        }
    }
}
