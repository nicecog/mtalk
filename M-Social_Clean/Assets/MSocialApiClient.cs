using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// M-Social server HTTP client (sign-in, mface-images CSV sync).
/// </summary>
public class MSocialApiClient : MonoBehaviour
{
    public enum ApiTarget
    {
        Local,
        Remote,
        Custom
    }

    public class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }

    public class ApiResult
    {
        public bool success;
        public long responseCode;
        public string text;
        public byte[] data;
        public string error;
    }

    public static MSocialApiClient Instance { get; private set; }

    public ApiTarget apiTarget = ApiTarget.Remote;
    public string localDesktopUrl = "http://127.0.0.1:8080/msocial";
    public string localAndroidEmulatorUrl = "http://10.0.2.2:8080/msocial";
    public string remoteUrl = "http://hudit.cafe24.com:8080/msocial";
    public string customUrl = "";

    string sessionCookie;

    public bool HasSession => !string.IsNullOrEmpty(sessionCookie);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("MSocialApiClient base URL: " + GetBaseUrl());
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static MSocialApiClient EnsureExists()
    {
        if (Instance != null)
            return Instance;

        var go = new GameObject(nameof(MSocialApiClient));
        return go.AddComponent<MSocialApiClient>();
    }

    public string GetBaseUrl()
    {
        string selectedUrl = remoteUrl;

        if (apiTarget == ApiTarget.Custom && !string.IsNullOrEmpty(customUrl))
            selectedUrl = customUrl;
        else if (apiTarget == ApiTarget.Local)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            selectedUrl = localAndroidEmulatorUrl;
#else
            selectedUrl = localDesktopUrl;
#endif
        }

        return selectedUrl.TrimEnd('/');
    }

    public string BuildUrl(string path)
    {
        if (string.IsNullOrEmpty(path))
            return GetBaseUrl();

        if (!path.StartsWith("/"))
            path = "/" + path;

        return GetBaseUrl() + path;
    }

    public void PrepareRequest(UnityWebRequest request, bool isJson = false)
    {
        if (isJson)
            request.SetRequestHeader("Content-Type", "application/json");

        if (!string.IsNullOrEmpty(sessionCookie))
            request.SetRequestHeader("Cookie", sessionCookie);

        if (request.url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            request.certificateHandler = new BypassCertificate();
    }

    public void CaptureSessionCookie(UnityWebRequest request)
    {
        var headers = request.GetResponseHeaders();
        if (headers == null)
            return;

        if (!headers.TryGetValue("SET-COOKIE", out var cookieHeader) &&
            !headers.TryGetValue("Set-Cookie", out cookieHeader))
            return;

        sessionCookie = cookieHeader.Split(';')[0];
        Debug.Log("Stored session cookie: " + sessionCookie);
    }

    static void FillResult(UnityWebRequest request, ApiResult result)
    {
        result.success = request.result == UnityWebRequest.Result.Success;
        result.responseCode = request.responseCode;
        result.data = request.downloadHandler != null ? request.downloadHandler.data : null;
        result.text = request.downloadHandler != null ? request.downloadHandler.text : "";
        result.error = request.error;
    }

    public IEnumerator Get(string path, ApiResult result, int timeoutSec = 20)
    {
        using (var request = UnityWebRequest.Get(BuildUrl(path)))
        {
            request.timeout = timeoutSec;
            PrepareRequest(request);
            yield return request.SendWebRequest();
            CaptureSessionCookie(request);
            FillResult(request, result);
        }
    }

    public IEnumerator PostJson(string path, string json, ApiResult result, int timeoutSec = 20)
    {
        using (var request = new UnityWebRequest(BuildUrl(path), UnityWebRequest.kHttpVerbPOST))
        {
            request.timeout = timeoutSec;
            var body = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            PrepareRequest(request, true);
            yield return request.SendWebRequest();
            CaptureSessionCookie(request);
            FillResult(request, result);
        }
    }

    public IEnumerator PutJson(string path, string json, ApiResult result, int timeoutSec = 20)
    {
        using (var request = new UnityWebRequest(BuildUrl(path), UnityWebRequest.kHttpVerbPUT))
        {
            request.timeout = timeoutSec;
            var body = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            PrepareRequest(request, true);
            yield return request.SendWebRequest();
            CaptureSessionCookie(request);
            FillResult(request, result);
        }
    }

    public IEnumerator PostMultipart(string path, List<IMultipartFormSection> formData, ApiResult result, int timeoutSec = 60)
    {
        using (var request = UnityWebRequest.Post(BuildUrl(path), formData))
        {
            request.timeout = timeoutSec;
            request.downloadHandler = new DownloadHandlerBuffer();
            PrepareRequest(request);
            yield return request.SendWebRequest();
            CaptureSessionCookie(request);
            FillResult(request, result);
        }
    }

    public IEnumerator Delete(string path, ApiResult result, int timeoutSec = 20)
    {
        using (var request = UnityWebRequest.Delete(BuildUrl(path)))
        {
            request.timeout = timeoutSec;
            request.downloadHandler = new DownloadHandlerBuffer();
            PrepareRequest(request);
            yield return request.SendWebRequest();
            CaptureSessionCookie(request);
            FillResult(request, result);
        }
    }

    public static string ExtractSeq(string responseText)
    {
        try
        {
            var parsed = JsonUtility.FromJson<LoginSeqResult>(responseText);
            if (parsed != null && parsed.seq > 0)
                return parsed.seq.ToString();
        }
        catch
        {
            // fall through
        }

        foreach (var part in responseText.Split(','))
        {
            if (!part.Contains("seq"))
                continue;

            return part.Split(':')[1].Replace("}", "").Replace("\"", "").Trim();
        }

        return "";
    }
}

[Serializable]
public class LoginSeqResult
{
    public int seq;
}
