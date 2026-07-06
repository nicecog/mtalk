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
    public Text adminText;

    MSocialApiClient api;
    bool loginBusy;

    static readonly Color ColorSuccess = new Color(0.1f, 0.55f, 0.15f);
    static readonly Color ColorError = new Color(0.85f, 0.15f, 0.1f);
    static readonly Color ColorInfo = new Color(0.2f, 0.2f, 0.2f);
    const string DefaultItemString = "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";

    public List<int> todayCode;
    public int[] SceneMaximum = {0, 2, 0, 3, 2, 1};
    public int playNum;
    public string fileName;
    public string Stage1Progress;
    public string Stage2Progress;
    public string ItemString;
    public bool isAdmin = true;
    public string pw = "spike1";
    public string name = "spike";
    public string email = "None";
    public string phone = "000";

    public String seq;
    Text _statusText;

    void Awake() {
        api = MSocialApiClient.EnsureExists();
        MSocialAppServices.EnsureExists().BindLogin(this);
        ResolveInputFields();
        EnsureStatusText();
    }

    void OnDisable() {
        loginBusy = false;
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

    void EnsureStatusText() {
        if (adminText != null) {
            _statusText = adminText;
            return;
        }
        var existing = transform.Find("LoginStatusText");
        if (existing != null) {
            _statusText = existing.GetComponent<Text>();
            if (_statusText != null)
                return;
        }
        var go = new GameObject("LoginStatusText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 40f);
        rt.sizeDelta = new Vector2(900f, 80f);
        _statusText = go.GetComponent<Text>();
        _statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _statusText.fontSize = 28;
        _statusText.alignment = TextAnchor.MiddleCenter;
        _statusText.color = ColorInfo;
        _statusText.raycastTarget = false;
        go.SetActive(false);
    }

    public void updateID() {
        SyncCredentials();
    }

    public void updatePW(Text pwText) {
        SyncCredentials();
    }

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
        MSocialStartupLog.Log("loginWithID clicked busy=" + loginBusy + " id=" + ID);
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

        playNum = 1;
        if (ID == "Test")
            playNum = 1;
        else if (ID == "admin")
            playNum = 8;

        Stage1Progress = DefaultItemString;
        playNum = playNum > 8 ? 8 : playNum;
        todayCode = new List<int>();
        ItemString = DefaultItemString;

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

        var hasResult = new MSocialApiClient.ApiResult();
        yield return api.Get("/api/users/accounts/id/has/" + l.id, hasResult);
        if (!hasResult.success) {
            ShowLoginError("서버 연결 실패: " + FormatApiError(hasResult));
            loginBusy = false;
            yield break;
        }

        string json = JsonUtility.ToJson(l);
        if (!hasResult.text.Contains("true")) {
            var adminSignIn = new MSocialApiClient.ApiResult();
            yield return api.PostJson("/api/users/sign-in",
                JsonUtility.ToJson(new user("super-admin", "super-admin!@#$")), adminSignIn);
            if (!IsHttpOk(adminSignIn)) {
                ShowLoginError("회원가입 준비 실패: " + FormatApiError(adminSignIn));
                loginBusy = false;
                yield break;
            }

            var createResult = new MSocialApiClient.ApiResult();
            yield return api.PostJson("/api/users/accounts", json, createResult);
            if (!IsHttpOk(createResult)) {
                ShowLoginError("회원가입 실패: " + FormatApiError(createResult));
                loginBusy = false;
                yield break;
            }

            yield return LogOut();
        }

        var loginResult = new MSocialApiClient.ApiResult();
        yield return api.PostJson("/api/users/sign-in",
            JsonUtility.ToJson(new user(l.id, l.password)), loginResult);
        if (!IsHttpOk(loginResult)) {
            ShowLoginError("로그인 실패: " + FormatLoginError(loginResult));
            loginBusy = false;
            yield break;
        }

        seq = MSocialApiClient.ExtractSeq(loginResult.text);
        if (string.IsNullOrEmpty(seq)) {
            ShowLoginError("로그인 응답 오류 (seq 없음): " + loginResult.text);
            loginBusy = false;
            yield break;
        }

        if (ID != "admin" && ID != "Test")
            yield return SyncProgressAfterLogin();

        ShowLoginSuccess("정상 로그인 되었습니다");
        yield return new WaitForSeconds(0.5f);

        MSocialMovieLibrary.EnsureLoaded();
        yield return MSocialSceneRouter.EnsureLoaded(MSocialSceneId.Stage1);

        var flowManager = MSocialAppServices.Instance != null ? MSocialAppServices.Instance.Flow : null;
        if (flowManager != null)
        {
            MSocialStartupLog.Log("Login -> Stage1SelectMap");
            flowManager.GoToPage(MSocialPageId.Stage1SelectMap);
        }
        else
        {
            var flow = GetComponent<ButtonFlow>();
            if (flow == null)
            {
                ShowLoginError("화면 전환 오류 (FlowManager 없음)");
                loginBusy = false;
                yield break;
            }
            yield return MSocialSceneRouter.ShowPage(MSocialPageId.Stage1SelectMap, flow);
        }

        loginBusy = false;
    }

    IEnumerator SyncProgressAfterLogin() {
        var progressOk = false;
        var progressError = "";
        MSocialProgressData progress = null;

        yield return MSocialProgressApi.SyncMyProgress(api, (ok, err, data) => {
            progressOk = ok;
            progressError = err;
            progress = data;
        });

        if (progressOk && progress != null) {
            ApplyServerProgress(progress);
            yield break;
        }

        Debug.LogWarning("[Login] Server progress failed (" + progressError + "); using local CSV fallback.");
        if (MSocialCsvStore.TryPrepareOnLogin(ID, out var record)) {
            playNum = record.dayCount;
            ItemString = record.progressString;
            Stage1Progress = ItemString;
            Stage2Progress = ItemString;
        } else {
            playNum = 1;
            ItemString = DefaultItemString;
            Stage1Progress = DefaultItemString;
            Stage2Progress = DefaultItemString;
        }
    }

    public void ApplyServerProgress(MSocialProgressData progress) {
        if (progress == null)
            return;

        playNum = progress.dayCount > 0 ? progress.dayCount : 1;
        ItemString = MSocialCsvStore.NormalizeProgressString(progress.progressString);
        Stage1Progress = ItemString;
        Stage2Progress = ItemString;
        MSocialCsvStore.TryUpdateProgress(ID, playNum, ItemString, progress.lastVisitDate);
        Debug.Log("[Login] Applied server progress dayCount=" + playNum + " progress=" + ItemString);
    }

    public void SyncProgressFromServer(Action<bool> onComplete = null) {
        StartCoroutine(SyncProgressFromServerCoroutine(onComplete));
    }

    public IEnumerator SyncProgressFromServerCoroutine(Action<bool> onComplete = null) {
        if (api == null || !api.HasSession || ID == "admin" || ID == "Test") {
            onComplete?.Invoke(false);
            yield break;
        }

        var progressOk = false;
        var progressError = "";
        MSocialProgressData progress = null;

        yield return MSocialProgressApi.SyncMyProgress(api, (ok, err, data) => {
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
        if (api == null || !api.HasSession || ID == "admin" || ID == "Test") {
            onComplete?.Invoke(false, "no session");
            yield break;
        }

        MSocialCsvStore.TryUpdateProgress(ID, playNum, ItemString);

        var ok = false;
        var err = "";
        yield return MSocialProgressApi.SaveMyProgress(api, playNum, ItemString, (success, error) => {
            ok = success;
            err = error;
        });

        if (!ok)
            Debug.LogWarning("[Login] SaveMyProgress failed: " + err);

        onComplete?.Invoke(ok, err);
    }

    static bool IsHttpOk(MSocialApiClient.ApiResult result) =>
        result.success && result.responseCode >= 200 && result.responseCode < 300;

    static string FormatApiError(MSocialApiClient.ApiResult result) {
        if (!string.IsNullOrEmpty(result.error))
            return result.error;
        if (!string.IsNullOrEmpty(result.text))
            return result.text;
        return "HTTP " + result.responseCode;
    }

    static string FormatLoginError(MSocialApiClient.ApiResult result) {
        var detail = FormatApiError(result);
        if (result.responseCode == 401)
            return "아이디 또는 비밀번호가 올바르지 않습니다. (" + detail + ")";
        return detail;
    }

    void ShowLoginMessage(string message) =>
        MSocialLoginAlert.Show(message, _statusText, ColorInfo);

    void ShowLoginSuccess(string message) =>
        MSocialLoginAlert.Show(message, _statusText, ColorSuccess);

    void ShowLoginError(string message) =>
        MSocialLoginAlert.Show(message, _statusText, ColorError);

    public IEnumerator LogOut() {
        var result = new MSocialApiClient.ApiResult();
        yield return api.Get("/api/users/sign-out", result);
        if (!result.success)
            Debug.Log(result.error + " " + result.text);
    }

    public void logUpload(String body) {
        String dateTime = ("" + System.DateTime.Now).Replace(':', '.');
        int i = dateTime.IndexOf(' ');
        var logDate = dateTime.Substring(0, i);
        var logTime = dateTime.Substring(i + 1, dateTime.Length - (i + 1));
        var csvLine = logDate + "," + logTime + "," + body;
        Debug.Log(csvLine);

        MSocialCsvStore.TryAppendLog(ID, csvLine);
        StartCoroutine(PostActivityLog(logDate, logTime, body));
    }

    IEnumerator PostActivityLog(string logDate, string logTime, string logBody) {
        if (api == null || !api.HasSession || ID == "admin" || ID == "Test")
            yield break;

        yield return MSocialProgressApi.PostActivityLog(api, logDate, logTime, logBody, (ok, err) => {
            if (!ok)
                Debug.LogWarning("[Login] PostActivityLog failed: " + err);
        });
    }

    public void itemChange() {
        ItemString = MSocialCsvStore.NormalizeProgressString(ItemString);
        Stage1Progress = ItemString;
        Stage2Progress = ItemString;
        MSocialCsvStore.TryUpdateProgress(ID, playNum, ItemString);
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
