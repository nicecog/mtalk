using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class MSocialProgressData {
    public int userAccountSeq;
    public int dayCount;
    public string progressString;
    public string lastVisitDate;
}

[Serializable]
class UpdateSocialProgressPayload {
    public int dayCount;
    public string progressString;
}

[Serializable]
class CreateSocialActivityLogPayload {
    public string logDate;
    public string logTime;
    public string logBody;
}

public static class MSocialProgressApi
{
    public static IEnumerator SyncMyProgress(MSocialApiClient api, Action<bool, string, MSocialProgressData> onComplete)
    {
        var result = new MSocialApiClient.ApiResult();
        yield return api.Get("/api/players/me/progress", result);

        if (!IsHttpOk(result)) {
            onComplete?.Invoke(false, FormatError(result), null);
            yield break;
        }

        var parsed = JsonUtility.FromJson<MSocialProgressData>(result.text);
        if (parsed == null) {
            onComplete?.Invoke(false, "Invalid progress response.", null);
            yield break;
        }

        onComplete?.Invoke(true, null, parsed);
    }

    public static IEnumerator SaveMyProgress(
        MSocialApiClient api,
        int dayCount,
        string progressString,
        Action<bool, string> onComplete)
    {
        var payload = JsonUtility.ToJson(new UpdateSocialProgressPayload {
            dayCount = dayCount,
            progressString = MSocialCsvStore.NormalizeProgressString(progressString)
        });

        var result = new MSocialApiClient.ApiResult();
        yield return api.PutJson("/api/players/me/progress", payload, result);

        if (!IsHttpOk(result)) {
            onComplete?.Invoke(false, FormatError(result));
            yield break;
        }

        onComplete?.Invoke(true, null);
    }

    public static IEnumerator PostActivityLog(
        MSocialApiClient api,
        string logDate,
        string logTime,
        string logBody,
        Action<bool, string> onComplete)
    {
        var payload = JsonUtility.ToJson(new CreateSocialActivityLogPayload {
            logDate = logDate,
            logTime = logTime,
            logBody = logBody ?? ""
        });

        var result = new MSocialApiClient.ApiResult();
        yield return api.PostJson("/api/players/me/activity-logs", payload, result);

        if (!IsHttpOk(result)) {
            onComplete?.Invoke(false, FormatError(result));
            yield break;
        }

        onComplete?.Invoke(true, null);
    }

    static bool IsHttpOk(MSocialApiClient.ApiResult result) =>
        result.success && result.responseCode >= 200 && result.responseCode < 300;

    static string FormatError(MSocialApiClient.ApiResult result)
    {
        if (!string.IsNullOrEmpty(result.error))
            return result.error;
        if (!string.IsNullOrEmpty(result.text))
            return result.text;
        return "HTTP " + result.responseCode;
    }
}
