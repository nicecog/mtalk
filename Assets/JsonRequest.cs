using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class user {
    public string id;
    public string password;

    public user(string _id, string _password) {
        id = _id;
        password = _password;
    }
}

[System.Serializable]
public class loginResult {
    public int seq;
}

[System.Serializable]
public class uploadedMovieResult {
    public string fileVideo;
}

public class JsonRequest : MonoBehaviour
{
    public enum ApiTarget {
        Local,
        Remote,
        Custom
    }

    public class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }

    public const string AccountNotFoundError = "ACCOUNT_NOT_FOUND";

    public ApiTarget apiTarget = ApiTarget.Local;
    public string localDesktopUrl = "http://127.0.0.1:8080/mbody";
    public string localAndroidEmulatorUrl = "http://10.0.2.2:8080/mbody";
    public string remoteUrl = "http://hudit.cafe24.com:8080/mbody";
    public string customUrl = "";
    public string ID = "spike1";
    public string pw = "spike1";

    private string seq;
    private string sessionCookie;

    void Awake() {
        Debug.Log("JsonRequest base URL: " + GetBaseUrl());
    }

    public void login(Action<bool, string> onComplete = null) {
        StartCoroutine(AdminLogin(onComplete));
    }

    private string GetBaseUrl() {
        string selectedUrl = remoteUrl;

        if (apiTarget == ApiTarget.Custom && customUrl != "") {
            selectedUrl = customUrl;
        } else if (apiTarget == ApiTarget.Local) {
            #if UNITY_ANDROID && !UNITY_EDITOR
                selectedUrl = localAndroidEmulatorUrl;
            #else
                selectedUrl = localDesktopUrl;
            #endif
        }

        return selectedUrl.TrimEnd('/');
    }

    private string BuildUrl(string path) => GetBaseUrl() + path;

    private void PrepareRequest(UnityWebRequest request, bool isJson = false) {
        if (isJson)
            request.SetRequestHeader("Content-Type", "application/json");

        if (!string.IsNullOrEmpty(sessionCookie))
            request.SetRequestHeader("Cookie", sessionCookie);

        if (request.url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            request.certificateHandler = new BypassCertificate();
    }

    private void CaptureSessionCookie(UnityWebRequest request) {
        Dictionary<string, string> headers = request.GetResponseHeaders();
        if (headers == null)
            return;

        if (!headers.TryGetValue("SET-COOKIE", out string cookieHeader) &&
            !headers.TryGetValue("Set-Cookie", out cookieHeader))
            return;

        sessionCookie = cookieHeader.Split(';')[0];
        Debug.Log("Stored session cookie: " + sessionCookie);
    }

    IEnumerator AdminLogin(Action<bool, string> onComplete) {
        var serverId = ID.Trim();

        using (UnityWebRequest w = UnityWebRequest.Get(BuildUrl("/api/users/accounts/id/has/" + serverId))) {
            PrepareRequest(w);
            yield return w.SendWebRequest();
            CaptureSessionCookie(w);
            if (w.result != UnityWebRequest.Result.Success) {
                CompleteLogin(onComplete, false, w.error + " " + w.downloadHandler.text);
                yield break;
            }

            if (!w.downloadHandler.text.Contains("true")) {
                CompleteLogin(onComplete, false, AccountNotFoundError);
                yield break;
            }

            string loginJson = JsonUtility.ToJson(new user(serverId, pw));
            bool loginOk = false;
            string loginError = "";
            yield return StartCoroutine(sendPost("/api/users/login", loginJson, true, (ok, err, result) => {
                loginOk = ok;
                loginError = err;
                if (ok && result != null)
                    seq = result.seq.ToString();
            }));

            if (loginOk)
                CompleteLogin(onComplete, true, null);
            else
                CompleteLogin(onComplete, false, loginError);
        }
    }

    static void CompleteLogin(Action<bool, string> onComplete, bool success, string error) {
        if (onComplete != null)
            onComplete(success, error);
    }

    public IEnumerator sendPost(string urlPost, string json, bool login, Action<bool, string, loginResult> onComplete) {
        using (UnityWebRequest www = new UnityWebRequest(BuildUrl(urlPost), UnityWebRequest.kHttpVerbPOST)) {
            byte[] jsonToSend = new UTF8Encoding().GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.uploadHandler.contentType = "application/json";
            www.downloadHandler = new DownloadHandlerBuffer();
            PrepareRequest(www, true);
            yield return www.SendWebRequest();
            CaptureSessionCookie(www);

            loginResult parsed = null;
            bool success = www.result == UnityWebRequest.Result.Success &&
                           www.responseCode >= 200 && www.responseCode < 300;

            if (!success) {
                var err = (string.IsNullOrEmpty(www.error) ? "" : www.error + " ") + www.downloadHandler.text;
                Debug.LogWarning("[JsonRequest] POST " + urlPost + " failed: " + err);
                if (onComplete != null)
                    onComplete(false, err.Trim(), null);
                yield break;
            }

            if (login) {
                parsed = JsonUtility.FromJson<loginResult>(www.downloadHandler.text);
                if (parsed == null || parsed.seq <= 0) {
                    if (onComplete != null)
                        onComplete(false, "Invalid login response.", null);
                    yield break;
                }
                seq = parsed.seq.ToString();
            }

            if (onComplete != null)
                onComplete(true, null, parsed);
        }
    }

    public void uploadMovie(string file) {
        StartCoroutine(upload(file));
    }

    IEnumerator upload(string file) {
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", File.ReadAllBytes(file.Replace('\\', '/')), file.Replace('\\', '/').Split('/').Last(), "video/mp4");
        UnityWebRequest www = UnityWebRequest.Post(BuildUrl("/api/files/attach"), form);
        www.downloadHandler = new DownloadHandlerBuffer();
        PrepareRequest(www);
        yield return www.SendWebRequest();
        CaptureSessionCookie(www);
        if (www.result != UnityWebRequest.Result.Success)
            Debug.Log(www.error + " " + www.downloadHandler.text);
        else
            Debug.Log("Form upload complete! : " + www.responseCode + " " + www.downloadHandler.text);

        string received = www.downloadHandler.text;
        uploadedMovieResult uploadResult = JsonUtility.FromJson<uploadedMovieResult>(received);
        string small = uploadResult != null && uploadResult.fileVideo != null && uploadResult.fileVideo != ""
            ? uploadResult.fileVideo
            : received.Split(':')[1].Split('\"')[1];
        movieFile mf = new movieFile();
        mf.userAccountSeq = int.Parse(seq);
        mf.fileVideo = small;
        mf.contentType = "post";
        string js = JsonUtility.ToJson(mf);
        UnityWebRequest w = new UnityWebRequest(BuildUrl("/api/subject-videos"), UnityWebRequest.kHttpVerbPOST);
        byte[] jsonToSend = new UTF8Encoding().GetBytes(js);
        w.uploadHandler = new UploadHandlerRaw(jsonToSend);
        w.uploadHandler.contentType = "application/json";
        w.downloadHandler = new DownloadHandlerBuffer();
        PrepareRequest(w, true);
        yield return w.SendWebRequest();
        CaptureSessionCookie(w);

        if (w.result != UnityWebRequest.Result.Success)
            Debug.Log(w.error + " " + w.downloadHandler.text);
        else
            Debug.Log("Form upload complete! : " + w.responseCode + " " + w.downloadHandler.text);
    }

    [System.Serializable]
    class movieFile {
        public int userAccountSeq;
        public string fileVideo;
        public int contentPoint = 0;
        public string contentType;
    }
}






