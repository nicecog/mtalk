using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Login : MonoBehaviour {
    public string ID;
    public Text inputText;
    public InputField idInput;
    public InputField pwInput;

    MFaceApiClient api;
    bool loginBusy;

    const string DefaultItemString = "FFFFFFFFFFFFFFFF";

    static readonly Color ColorSuccess = new Color(0.1f, 0.55f, 0.15f);
    static readonly Color ColorError = new Color(0.85f, 0.15f, 0.1f);
    static readonly Color ColorInfo = new Color(0.2f, 0.2f, 0.2f);

    void Awake() {
        api = MFaceApiClient.EnsureExists();
        ResolveInputFields();
    }

    void ResolveInputFields() {
        var inputs = GetComponentsInChildren<InputField>(true);
        foreach (var input in inputs) {
            if (idInput == null && input.name == "InputField")
                idInput = input;
            else if (pwInput == null && input.name == "InputField (1)")
                pwInput = input;
        }
    }

    public void updateID() {
        SyncCredentials();
    }

    public void updatePW(Text pwText) {
        if (pwInput != null)
            pw = pwInput.text;
        else if (pwText != null && pwText.text != "")
            pw = pwText.text;
    }

    public Text adminText;

    private void OnEnable() {
        loginBusy = false;
        SyncCredentials();
        ItemString = DefaultItemString;
    }

    void SyncCredentials() {
        if (idInput != null)
            ID = idInput.text.Trim();
        else if (inputText != null)
            ID = inputText.text.Trim();

        if (pwInput != null)
            pw = pwInput.text;
    }

    public void loginWithID() {
        if (loginBusy)
            return;

        SyncCredentials();

        if (string.IsNullOrEmpty(ID)) {
            ShowLoginError("아이디를 입력해 주세요.");
            return;
        }

        if (string.IsNullOrEmpty(pw)) {
            ShowLoginError("비밀번호를 입력해 주세요.");
            return;
        }

        ItemString = DefaultItemString;
        if (adminText != null)
            adminText.gameObject.SetActive(false);
        playNum = 1;
        if (ID == "test") {
            ItemString = DefaultItemString;
            playNum = 1;
        }
        else if (ID == "admin") {
            ItemString = "TTTTTTTTTTTTTTTT";
            playNum = 8;
            if (adminText != null)
                adminText.gameObject.SetActive(true);
        }

        login();
    }

    public void login() {
        StartCoroutine(AdminLogin());
    }

    IEnumerator AdminLogin() {
        loginBusy = true;
        ShowLoginMessage("로그인 중...");

        loginUser l = new loginUser();
        l.id = ID.ToLower();
        l.password = pw;
        l.name = name;

        var hasResult = new MFaceApiClient.ApiResult();
        yield return api.Get("/api/users/accounts/id/has/" + l.id, hasResult);
        if (!hasResult.success) {
            ShowLoginError("서버 연결 실패: " + FormatApiError(hasResult));
            loginBusy = false;
            yield break;
        }

        string json = JsonUtility.ToJson(l);
        if (!hasResult.text.Contains("true")) {
            var adminSignIn = new MFaceApiClient.ApiResult();
            yield return api.PostJson("/api/users/sign-in",
                JsonUtility.ToJson(new user("super-admin", "super-admin!@#$")), adminSignIn);
            if (!IsHttpOk(adminSignIn)) {
                ShowLoginError("회원가입 준비 실패: " + FormatApiError(adminSignIn));
                loginBusy = false;
                yield break;
            }

            var createResult = new MFaceApiClient.ApiResult();
            yield return api.PostJson("/api/users/accounts", json, createResult);
            if (!IsHttpOk(createResult)) {
                ShowLoginError("회원가입 실패: " + FormatApiError(createResult));
                loginBusy = false;
                yield break;
            }

            yield return LogOut();
        }

        var loginResult = new MFaceApiClient.ApiResult();
        yield return api.PostJson("/api/users/sign-in",
            JsonUtility.ToJson(new user(l.id, l.password)), loginResult);
        if (!IsHttpOk(loginResult)) {
            ShowLoginError("로그인 실패: " + FormatLoginError(loginResult));
            loginBusy = false;
            yield break;
        }

        seq = MFaceApiClient.ExtractSeq(loginResult.text);
        if (string.IsNullOrEmpty(seq)) {
            ShowLoginError("로그인 응답 오류 (seq 없음): " + loginResult.text);
            loginBusy = false;
            yield break;
        }

        if (ID != "admin")
            yield return SyncProgressAfterLogin();

        ShowLoginSuccess("정상 로그인 되었습니다");
        yield return new WaitForSeconds(1.2f);

        var flow = GetComponent<ButtonFlow>();
        if (flow == null || flow.NextPage == null) {
            ShowLoginError("화면 전환 오류 (ButtonFlow 설정 확인)");
            loginBusy = false;
            yield break;
        }

        flow.ClickNext();
        loginBusy = false;
    }

    IEnumerator SyncProgressAfterLogin() {
        var progressOk = false;
        var progressError = "";
        MFaceProgressData progress = null;

        yield return MFaceProgressApi.SyncMyProgress(api, (ok, err, data) => {
            progressOk = ok;
            progressError = err;
            progress = data;
        });

        if (progressOk && progress != null) {
            ApplyServerProgress(progress);
            yield break;
        }

        Debug.LogWarning("[Login] Server progress failed (" + progressError + "); using local CSV fallback.");
        if (MFaceCsvStore.TryPrepareOnLogin(ID, out var record)) {
            playNum = record.dayCount;
            ItemString = record.itemString;
        } else {
            playNum = 1;
            ItemString = DefaultItemString;
        }
    }

    public void ApplyServerProgress(MFaceProgressData progress) {
        if (progress == null)
            return;

        playNum = progress.dayCount > 0 ? progress.dayCount : 1;
        ItemString = MFaceCsvStore.NormalizeItemString(progress.itemString);
        MFaceCsvStore.TryUpdateProgress(ID, playNum, ItemString, progress.lastVisitDate);
        Debug.Log("[Login] Applied server progress dayCount=" + playNum + " itemString=" + ItemString);
    }

    public void SyncProgressFromServer(Action<bool> onComplete = null) {
        StartCoroutine(SyncProgressFromServerCoroutine(onComplete));
    }

    public IEnumerator SyncProgressFromServerCoroutine(Action<bool> onComplete = null) {
        if (api == null || !api.HasSession || ID == "admin") {
            onComplete?.Invoke(false);
            yield break;
        }

        var progressOk = false;
        var progressError = "";
        MFaceProgressData progress = null;

        yield return MFaceProgressApi.SyncMyProgress(api, (ok, err, data) => {
            progressOk = ok;
            progressError = err;
            progress = data;
        });

        if (!progressOk || progress == null) {
            Debug.LogWarning("[Login] SyncProgressFromServer failed: " + progressError);
            onComplete?.Invoke(false);
            yield break;
        }

        ApplyServerProgress(progress);
        onComplete?.Invoke(true);
    }

    public void SaveProgressToServer(Action<bool, string> onComplete = null) {
        StartCoroutine(SaveProgressToServerCoroutine(onComplete));
    }

    IEnumerator SaveProgressToServerCoroutine(Action<bool, string> onComplete) {
        if (api == null || !api.HasSession || ID == "admin") {
            onComplete?.Invoke(false, "no session");
            yield break;
        }

        MFaceCsvStore.TryUpdateProgress(ID, playNum, ItemString);

        var ok = false;
        var err = "";
        yield return MFaceProgressApi.SaveMyProgress(api, playNum, ItemString, (success, error) => {
            ok = success;
            err = error;
        });

        if (!ok)
            Debug.LogWarning("[Login] SaveMyProgress failed: " + err);

        onComplete?.Invoke(ok, err);
    }

    static bool IsHttpOk(MFaceApiClient.ApiResult result) =>
        result.success && result.responseCode >= 200 && result.responseCode < 300;

    static string FormatApiError(MFaceApiClient.ApiResult result) {
        if (!string.IsNullOrEmpty(result.error))
            return result.error;
        if (!string.IsNullOrEmpty(result.text))
            return result.text;
        return "HTTP " + result.responseCode;
    }

    static string FormatLoginError(MFaceApiClient.ApiResult result) {
        var detail = FormatApiError(result);
        if (result.responseCode == 401)
            return "아이디 또는 비밀번호가 올바르지 않습니다. (" + detail + ")";
        return detail;
    }

    void ShowLoginMessage(string message) =>
        MFaceLoginAlert.Show(message, adminText, ColorInfo);

    void ShowLoginSuccess(string message) =>
        MFaceLoginAlert.Show(message, adminText, ColorSuccess);

    void ShowLoginError(string message) =>
        MFaceLoginAlert.Show(message, adminText, ColorError);

    public IEnumerator LogOut() {
        var result = new MFaceApiClient.ApiResult();
        yield return api.Get("/api/users/sign-out", result);
        if (!result.success)
            Debug.Log(result.error + " " + result.text);
    }

    public String seq;

    public int playNum;
    public string fileName;
    public string ItemString;

    public bool isAdmin = true;
    public string pw = "spike1";
    public string name = "spike";
    public string email = "None";
    public string phone = "000";

    public void logUpload(String body) {
        String dateTime = ("" + System.DateTime.Now).Replace(':', '.');
        int i = dateTime.IndexOf(' ');
        var logDate = dateTime.Substring(0, i);
        var logTime = dateTime.Substring(i + 1, dateTime.Length - (i + 1));
        var csvLine = logDate + "," + logTime + "," + body;

        MFaceCsvStore.TryAppendLog(ID, csvLine);
        StartCoroutine(PostActivityLog(logDate, logTime, body));
    }

    IEnumerator PostActivityLog(string logDate, string logTime, string logBody) {
        if (api == null || !api.HasSession || ID == "admin")
            yield break;

        yield return MFaceProgressApi.PostActivityLog(api, logDate, logTime, logBody, (ok, err) => {
            if (!ok)
                Debug.LogWarning("[Login] PostActivityLog failed: " + err);
        });
    }

    public void itemChange() {
        ItemString = MFaceCsvStore.NormalizeItemString(ItemString);
        MFaceCsvStore.TryUpdateProgress(ID, playNum, ItemString);
        SaveProgressToServer();
    }
}

[System.Serializable]
public class user {
    public string id;
    public string password;

    public user(string _id, string _password) {
        id = _id;
        password = _password;
    }
}

public class loginUser {
    public int level = 1;
    public string domain = "spike";
    public string id = "spike2";
    public string password = "spike!@#$";
    public string name = "스파이크";
    public string email = "spike2@gmail.com";
    public string phone = "010-0000-0000";
    public string alias = "null";
}
