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
public class PlayerProgressData {
    public int userAccountSeq;
    public int playTimes;
    public int bodyPlayTimes;
    public string danceStar;
    public string lastVisitDate;
}

[System.Serializable]
class UpdatePlayerProgressPayload {
    public int playTimes;
    public int bodyPlayTimes;
    public string danceStar;
}

[System.Serializable]
class TrainingResultPayload {
    public string recordedAt;
    public string resultType;
    public string trial;
    public string point;
    public string details;
}

[System.Serializable]
public class uploadedMovieResult {
    public string fileVideo;
}

public class JsonRequest : MonoBehaviour
{
    public enum ApiTarget {
        Local,
        LocalMtalk,
        Remote,
        Custom
    }

    public class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }

    public const string AccountNotFoundError = "ACCOUNT_NOT_FOUND";

    public ApiTarget apiTarget = ApiTarget.Remote;
    public string localDesktopUrl = "http://127.0.0.1:21250";
    public string localAndroidEmulatorUrl = "http://10.0.2.2:21250";
    public string localMtalkDesktopUrl = "http://127.0.0.1:8080/mbody";
    public string localMtalkAndroidEmulatorUrl = "http://10.0.2.2:8080/mbody";
    public string remoteUrl = "http://hudit.cafe24.com:8080/mbody";
    public string customUrl = "";
    public int requestTimeoutSeconds = 20;
    public string ID = "spike1";
    public string pw = "spike1";

    private string seq;
    private string sessionCookie;

    void Awake() {
        Debug.Log("[JsonRequest] apiTarget=" + apiTarget + " base URL=" + GetBaseUrl());
    }

    public bool HasSession => !string.IsNullOrEmpty(sessionCookie);

    public string GetUserSeq() => seq;

    public IEnumerator SyncMyProgress(Action<bool, string, PlayerProgressData> onComplete) {
        using (UnityWebRequest www = UnityWebRequest.Get(BuildUrl("/api/players/me/progress"))) {
            www.downloadHandler = new DownloadHandlerBuffer();
            PrepareRequest(www);
            yield return www.SendWebRequest();
            CaptureSessionCookie(www);

            bool success = www.result == UnityWebRequest.Result.Success &&
                           www.responseCode >= 200 && www.responseCode < 300;
            if (!success) {
                var err = (string.IsNullOrEmpty(www.error) ? "" : www.error + " ") + www.downloadHandler.text;
                if (onComplete != null)
                    onComplete(false, err.Trim(), null);
                yield break;
            }

            var parsed = JsonUtility.FromJson<PlayerProgressData>(www.downloadHandler.text);
            if (parsed == null) {
                if (onComplete != null)
                    onComplete(false, "Invalid progress response.", null);
                yield break;
            }

            if (parsed.userAccountSeq > 0)
                seq = parsed.userAccountSeq.ToString();

            if (onComplete != null)
                onComplete(true, null, parsed);
        }
    }

    public IEnumerator SaveMyProgress(int playTimes, int bodyPlayTimes, string danceStar, Action<bool, string> onComplete) {
        var payload = JsonUtility.ToJson(new UpdatePlayerProgressPayload {
            playTimes = playTimes,
            bodyPlayTimes = bodyPlayTimes,
            danceStar = danceStar
        });

        using (UnityWebRequest www = new UnityWebRequest(BuildUrl("/api/players/me/progress"), UnityWebRequest.kHttpVerbPUT)) {
            byte[] jsonToSend = new UTF8Encoding().GetBytes(payload);
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.uploadHandler.contentType = "application/json";
            www.downloadHandler = new DownloadHandlerBuffer();
            PrepareRequest(www, true);
            yield return www.SendWebRequest();
            CaptureSessionCookie(www);

            bool success = www.result == UnityWebRequest.Result.Success &&
                           www.responseCode >= 200 && www.responseCode < 300;
            if (!success) {
                var err = (string.IsNullOrEmpty(www.error) ? "" : www.error + " ") + www.downloadHandler.text;
                if (onComplete != null)
                    onComplete(false, err.Trim());
                yield break;
            }

            if (onComplete != null)
                onComplete(true, null);
        }
    }

    public void SaveMyProgressAsync(int playTimes, int bodyPlayTimes, string danceStar, Action<bool, string> onComplete = null) {
        StartCoroutine(SaveMyProgress(playTimes, bodyPlayTimes, danceStar, onComplete));
    }

    public void PostTrainingResultAsync(string msg, Action<bool, string> onComplete = null) {
        StartCoroutine(PostTrainingResult(msg, onComplete));
    }

    public IEnumerator PostTrainingResult(string msg, Action<bool, string> onComplete) {
        if (!TryParseTrainingMessage(msg, out var payload)) {
            if (onComplete != null)
                onComplete(false, "Invalid training message.");
            yield break;
        }

        string json = JsonUtility.ToJson(payload);
        using (UnityWebRequest www = new UnityWebRequest(BuildUrl("/api/players/me/training-results"), UnityWebRequest.kHttpVerbPOST)) {
            byte[] jsonToSend = new UTF8Encoding().GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.uploadHandler.contentType = "application/json";
            www.downloadHandler = new DownloadHandlerBuffer();
            PrepareRequest(www, true);
            yield return www.SendWebRequest();
            CaptureSessionCookie(www);

            bool success = www.result == UnityWebRequest.Result.Success &&
                           www.responseCode >= 200 && www.responseCode < 300;
            if (!success) {
                var err = (string.IsNullOrEmpty(www.error) ? "" : www.error + " ") + www.downloadHandler.text;
                if (onComplete != null)
                    onComplete(false, err.Trim());
                yield break;
            }

            if (onComplete != null)
                onComplete(true, null);
        }
    }

    static bool TryParseTrainingMessage(string msg, out TrainingResultPayload payload) {
        payload = null;
        if (string.IsNullOrWhiteSpace(msg))
            return false;

        var parts = msg.Split(',');
        if (parts.Length < 3)
            return false;

        payload = new TrainingResultPayload {
            recordedAt = DateTime.Now.ToString("yy-MM-dd HH:mm:ss"),
            resultType = parts[0].Trim(),
            trial = parts[1].Trim(),
            point = parts[2].Trim(),
            details = parts.Length > 3 ? string.Join(",", parts, 3, parts.Length - 3) : ""
        };
        return true;
    }

    public void login(Action<bool, string> onComplete = null) {
        StartCoroutine(AdminLogin(onComplete));
    }

    private string GetBaseUrl() {
        if (apiTarget == ApiTarget.Custom && !string.IsNullOrWhiteSpace(customUrl))
            return customUrl.TrimEnd('/');

        if (apiTarget == ApiTarget.Remote)
            return remoteUrl.TrimEnd('/');

#if UNITY_ANDROID && !UNITY_EDITOR
        bool isEmulator = IsAndroidEmulator();
        if (apiTarget == ApiTarget.LocalMtalk)
            return (isEmulator ? localMtalkAndroidEmulatorUrl : remoteUrl).TrimEnd('/');
        if (apiTarget == ApiTarget.Local)
            return (isEmulator ? localAndroidEmulatorUrl : remoteUrl).TrimEnd('/');
#endif

        if (apiTarget == ApiTarget.LocalMtalk)
            return localMtalkDesktopUrl.TrimEnd('/');

        return localDesktopUrl.TrimEnd('/');
    }

    static bool IsAndroidEmulator() {
        string model = SystemInfo.deviceModel ?? "";
        string deviceName = SystemInfo.deviceName ?? "";
        return model.IndexOf("google_sdk", StringComparison.OrdinalIgnoreCase) >= 0
               || model.IndexOf("Emulator", StringComparison.OrdinalIgnoreCase) >= 0
               || model.IndexOf("Android SDK built for", StringComparison.OrdinalIgnoreCase) >= 0
               || deviceName.IndexOf("emulator", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private string BuildUrl(string path) => GetBaseUrl() + path;

    private void PrepareRequest(UnityWebRequest request, bool isJson = false) {
        if (requestTimeoutSeconds > 0)
            request.timeout = requestTimeoutSeconds;

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
        Debug.Log("[JsonRequest] Login start id=" + serverId + " url=" + GetBaseUrl());

        using (UnityWebRequest w = UnityWebRequest.Get(BuildUrl("/api/users/accounts/id/has/" + UnityWebRequest.EscapeURL(serverId)))) {
            PrepareRequest(w);
            yield return w.SendWebRequest();
            CaptureSessionCookie(w);
            if (w.result != UnityWebRequest.Result.Success) {
                CompleteLogin(onComplete, false, FormatRequestError(w, "계정 확인"));
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

            if (loginOk) {
                Debug.Log("[JsonRequest] Login success seq=" + seq);
                CompleteLogin(onComplete, true, null);
            } else {
                CompleteLogin(onComplete, false, loginError);
            }
        }
    }

    static string FormatRequestError(UnityWebRequest request, string action) {
        var body = request.downloadHandler != null ? request.downloadHandler.text : "";
        var message = string.IsNullOrEmpty(request.error) ? body : request.error + " " + body;
        message = message.Trim();
        if (string.IsNullOrEmpty(message))
            message = action + " request failed (" + request.responseCode + ")";
        return message;
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
        var path = file.Replace('\\', '/');
        if (!File.Exists(path)) {
            Debug.LogWarning("[JsonRequest] upload missing file: " + path);
            yield break;
        }

        // Read file then drop local refs ASAP — large stage-3 clips OOM Tab A8 if kept around.
        byte[] bytes = null;
        try {
            bytes = File.ReadAllBytes(path);
        } catch (Exception ex) {
            Debug.LogWarning("[JsonRequest] upload read failed: " + ex.Message);
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", bytes, path.Split('/').Last(), "video/mp4");
        bytes = null;

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






