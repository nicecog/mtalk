using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class MFaceProgressData {
    public int userAccountSeq;
    public int dayCount;
    public string itemString;
    public string lastVisitDate;
}

[Serializable]
class UpdateFaceProgressPayload {
    public int dayCount;
    public string itemString;
}

[Serializable]
class CreateFaceActivityLogPayload {
    public string logDate;
    public string logTime;
    public string logBody;
}

public static class MFaceProgressApi
{
    public static IEnumerator SyncMyProgress(MFaceApiClient api, Action<bool, string, MFaceProgressData> onComplete)
    {
        var result = new MFaceApiClient.ApiResult();
        yield return api.Get("/api/players/me/progress", result);

        if (!IsHttpOk(result)) {
            onComplete?.Invoke(false, FormatError(result), null);
            yield break;
        }

        var parsed = JsonUtility.FromJson<MFaceProgressData>(result.text);
        if (parsed == null) {
            onComplete?.Invoke(false, "Invalid progress response.", null);
            yield break;
        }

        onComplete?.Invoke(true, null, parsed);
    }

    public static IEnumerator SaveMyProgress(
        MFaceApiClient api,
        int dayCount,
        string itemString,
        Action<bool, string> onComplete)
    {
        var payload = JsonUtility.ToJson(new UpdateFaceProgressPayload {
            dayCount = dayCount,
            itemString = MFaceCsvStore.NormalizeItemString(itemString)
        });

        var result = new MFaceApiClient.ApiResult();
        yield return api.PutJson("/api/players/me/progress", payload, result);

        if (!IsHttpOk(result)) {
            onComplete?.Invoke(false, FormatError(result));
            yield break;
        }

        onComplete?.Invoke(true, null);
    }

    public static IEnumerator PostActivityLog(
        MFaceApiClient api,
        string logDate,
        string logTime,
        string logBody,
        Action<bool, string> onComplete)
    {
        var payload = JsonUtility.ToJson(new CreateFaceActivityLogPayload {
            logDate = logDate,
            logTime = logTime,
            logBody = logBody ?? ""
        });

        var result = new MFaceApiClient.ApiResult();
        yield return api.PostJson("/api/players/me/activity-logs", payload, result);

        if (!IsHttpOk(result)) {
            onComplete?.Invoke(false, FormatError(result));
            yield break;
        }

        onComplete?.Invoke(true, null);
    }

    static bool IsHttpOk(MFaceApiClient.ApiResult result) =>
        result.success && result.responseCode >= 200 && result.responseCode < 300;

    static string FormatError(MFaceApiClient.ApiResult result)
    {
        if (!string.IsNullOrEmpty(result.error))
            return result.error;
        if (!string.IsNullOrEmpty(result.text))
            return result.text;
        return "HTTP " + result.responseCode;
    }
}
