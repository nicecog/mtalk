using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class LoginForm : MonoBehaviour {
    public JsonRequest jr;
    private string savePath;

    public Image mask;
    public Button nextButton;
    public string ID;
    public ButtonFlow bf;

    public InputField InputField;
    [SerializeField] InputField passwordInputField;
    [SerializeField] float passwordFieldOffsetY = -110f;
    [SerializeField] GameObject loginAlertPanel;
    [SerializeField] Text loginAlertText;
    [SerializeField] Button loginAlertOkButton;

    public int idx;
    public int playTimes;
    public string danceStar;
    public DanceManager dm;

    bool isSubmitting;

    void Awake() {
        DestroyUnityKeyboardIfAny();
        EnsurePasswordField();
        EnsureAlertPopup();
        ConfigureInputFieldsForSystemKeyboard();
    }

    void Update() {
        if (mask != null)
            mask.enabled = false;
    }

    void DestroyUnityKeyboardIfAny() {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null) {
            var existing = canvas.transform.Find("LoginUnityKeyboard");
            if (existing != null)
                Destroy(existing.gameObject);
        }

        var local = transform.Find("LoginUnityKeyboard");
        if (local != null)
            Destroy(local.gameObject);
    }

    void EnsurePasswordField() {
        if (passwordInputField != null || InputField == null)
            return;

        var clone = Instantiate(InputField, InputField.transform.parent);
        clone.name = "PasswordInputField";
        var idRect = InputField.GetComponent<RectTransform>();
        var passwordRect = clone.GetComponent<RectTransform>();
        passwordRect.anchorMin = idRect.anchorMin;
        passwordRect.anchorMax = idRect.anchorMax;
        passwordRect.pivot = idRect.pivot;
        passwordRect.sizeDelta = idRect.sizeDelta;
        passwordRect.anchoredPosition = idRect.anchoredPosition + new Vector2(0f, passwordFieldOffsetY);
        passwordRect.SetAsLastSibling();

        passwordInputField = clone.GetComponent<InputField>();
        passwordInputField.text = "";
        ConfigureField(passwordInputField, isPassword: true);
    }

    void ConfigureInputFieldsForSystemKeyboard() {
        // Use Android system keyboard via normal InputField activation.
        if (InputField != null)
            ConfigureField(InputField, isPassword: false);
        if (passwordInputField != null)
            ConfigureField(passwordInputField, isPassword: true);
    }

    void ConfigureField(InputField field, bool isPassword) {
        // Remove any previous tap-button overlay that blocked InputField.
        var tap = field.transform.Find("FieldTapButton");
        if (tap != null)
            Destroy(tap.gameObject);

        field.shouldHideMobileInput = false;
        field.readOnly = false;
        field.interactable = true;
        field.enabled = true;
        field.navigation = new Navigation { mode = Navigation.Mode.None };

        if (isPassword) {
            field.contentType = InputField.ContentType.Password;
            field.inputType = InputField.InputType.Password;
            field.keyboardType = TouchScreenKeyboardType.Default;
            field.asteriskChar = '●';
            // LoginUI art already draws "Password"; extra placeholder text stacks on top.
            HidePlaceholder(field);
        } else {
            field.contentType = InputField.ContentType.Standard;
            field.inputType = InputField.InputType.Standard;
            field.keyboardType = TouchScreenKeyboardType.ASCIICapable;
            if (field.placeholder is Text idPlaceholder) {
                idPlaceholder.raycastTarget = false;
                // Avoid cloning leftover password label onto the ID field.
                if (string.Equals(idPlaceholder.text, "Password", System.StringComparison.OrdinalIgnoreCase))
                    idPlaceholder.text = "";
            }
        }

        var text = field.textComponent;
        if (text != null) {
            text.raycastTarget = false;
            text.supportRichText = false;
        }

        var image = field.targetGraphic as Graphic;
        if (image != null) {
            image.raycastTarget = true;
            image.color = Color.white;
        }
    }

    static void HidePlaceholder(InputField field) {
        if (field.placeholder == null)
            return;

        if (field.placeholder is Text text) {
            text.text = "";
            text.enabled = false;
            text.raycastTarget = false;
        }

        field.placeholder.gameObject.SetActive(false);
    }

    void HideLoginChrome() {
        for (var i = 0; i < transform.childCount; i++) {
            var child = transform.GetChild(i);
            if (child.name.StartsWith("Quit", System.StringComparison.Ordinal))
                child.gameObject.SetActive(false);
        }

        var loginUi = transform.Find("LoginUI");
        if (loginUi == null)
            return;

        for (var i = 0; i < loginUi.childCount; i++) {
            var child = loginUi.GetChild(i);
            if (child.name.StartsWith("Quit", System.StringComparison.Ordinal))
                child.gameObject.SetActive(false);
        }
    }

    void EnsureOpaqueLoginBackdrop() {
        for (var i = 0; i < transform.childCount; i++) {
            var child = transform.GetChild(i);
            if (child.name != "Image")
                continue;

            var image = child.GetComponent<Image>();
            if (image == null)
                continue;

            var color = image.color;
            color.a = 1f;
            image.color = color;
            image.raycastTarget = false;
        }
    }

    void DisableLoginRaycastBlockers() {
        for (var i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (child.name != "Image")
                continue;

            var image = child.GetComponent<Image>();
            if (image != null)
                image.raycastTarget = false;
        }

        var loginUi = transform.Find("LoginUI");
        if (loginUi == null)
            return;

        var panel = loginUi.GetComponent<Image>();
        if (panel != null)
            panel.raycastTarget = false;
    }

    bool CanSubmit() {
        if (isSubmitting)
            return false;
        if (InputField == null || string.IsNullOrWhiteSpace(InputField.text))
            return false;
        if (passwordInputField == null || string.IsNullOrWhiteSpace(passwordInputField.text))
            return false;
        return true;
    }

    public void clickButton() {
        if (isSubmitting)
            return;

        if (InputField != null)
            InputField.DeactivateInputField();
        if (passwordInputField != null)
            passwordInputField.DeactivateInputField();

        if (InputField == null || string.IsNullOrWhiteSpace(InputField.text)) {
            ShowLoginAlert("아이디를 입력해 주세요.");
            return;
        }

        if (passwordInputField == null || string.IsNullOrWhiteSpace(passwordInputField.text)) {
            ShowLoginAlert("비밀번호를 입력해 주세요.");
            return;
        }

        StartCoroutine(SubmitLoginCoroutine());
    }

    IEnumerator SubmitLoginCoroutine() {
        if (jr == null) {
            jr = FindFirstObjectByType<JsonRequest>(FindObjectsInactive.Include);
        }
        if (jr == null) {
            ShowLoginAlert("서버 연결 설정(JsonRequest)을 찾을 수 없습니다.");
            yield break;
        }

        isSubmitting = true;
        SetLoginInteractable(false);
        HideLoginAlert();

        var playerId = InputField.text.Trim();
        jr.ID = playerId;
        jr.pw = passwordInputField.text;

        var loginDone = false;
        var loginSuccess = false;
        var loginError = "";
        jr.login((success, error) => {
            loginDone = true;
            loginSuccess = success;
            loginError = error;
        });

        while (!loginDone)
            yield return null;

        if (!loginSuccess) {
            isSubmitting = false;
            SetLoginInteractable(true);
            ShowLoginAlert(FormatLoginError(loginError));
            yield break;
        }

        var progressOk = false;
        var progressError = "";
        PlayerProgressData progress = null;
        yield return jr.SyncMyProgress((ok, err, data) => {
            progressOk = ok;
            progressError = err;
            progress = data;
        });

        if (progressOk && progress != null) {
            ApplyServerProgress(progress);
        } else if (PlayerCsvStore.TryPrepareOnLogin(playerId, out var record)) {
            ID = record.id;
            idx = record.idx;
            playTimes = record.playTimes;
            danceStar = record.danceStar;
            savePath = record.savePath;
            dm.ID = playerId;
            dm.playTimes = playTimes;
            dm.danceStar = danceStar;
            MBodyDiagLog.Warn("Login", $"Server progress failed ({progressError}); using local CSV fallback.");
        } else {
            isSubmitting = false;
            SetLoginInteractable(true);
            ShowLoginAlert("플레이어 진행 데이터를 불러오지 못했습니다.");
            yield break;
        }

        isSubmitting = false;
        SetLoginInteractable(true);
        HideLoginAlert();
        MBodyDiagLog.Step("Login", $"Success id={playerId} -> navigate via ButtonFlow");
        bf.ClickNext();
    }

    void ApplyBodyPlayTimes(int bodyPlayTimes) {
        var bm = FindObjectOfType<BodyManager>();
        if (bm != null)
            bm.playTimes = bodyPlayTimes;
    }

    int GetBodyPlayTimes() {
        var bm = FindObjectOfType<BodyManager>();
        return bm != null ? bm.playTimes : 0;
    }

    void EnsureAlertPopup() {
        if (loginAlertPanel != null)
            return;

        // Parent to canvas so the dialog is never clipped/overlapped by LoginUI fields.
        var canvas = GetComponentInParent<Canvas>();
        var parent = canvas != null ? canvas.transform : transform;
        var font = GetUiFont();

        loginAlertPanel = new GameObject("LoginAlertPanel", typeof(RectTransform));
        loginAlertPanel.transform.SetParent(parent, false);
        var panelRect = loginAlertPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var backdrop = loginAlertPanel.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.65f);
        backdrop.raycastTarget = true;

        var box = new GameObject("AlertBox", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(loginAlertPanel.transform, false);
        var boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        // Sit above the password field (password is near screen center).
        boxRect.anchoredPosition = new Vector2(0f, 160f);
        boxRect.sizeDelta = new Vector2(980f, 460f);
        box.GetComponent<Image>().color = Color.white;
        box.GetComponent<Image>().raycastTarget = true;

        var messageGo = new GameObject("Message", typeof(RectTransform), typeof(Text));
        messageGo.transform.SetParent(box.transform, false);
        var messageRect = messageGo.GetComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0.06f, 0.38f);
        messageRect.anchorMax = new Vector2(0.94f, 0.92f);
        messageRect.offsetMin = Vector2.zero;
        messageRect.offsetMax = Vector2.zero;
        loginAlertText = messageGo.GetComponent<Text>();
        loginAlertText.font = font;
        loginAlertText.fontSize = 44;
        loginAlertText.alignment = TextAnchor.MiddleCenter;
        loginAlertText.color = Color.black;
        loginAlertText.horizontalOverflow = HorizontalWrapMode.Wrap;
        loginAlertText.verticalOverflow = VerticalWrapMode.Overflow;
        loginAlertText.raycastTarget = false;

        var okGo = new GameObject("OkButton", typeof(RectTransform), typeof(Image), typeof(Button));
        okGo.transform.SetParent(box.transform, false);
        var okRect = okGo.GetComponent<RectTransform>();
        okRect.anchorMin = okRect.anchorMax = new Vector2(0.5f, 0f);
        okRect.pivot = new Vector2(0.5f, 0f);
        okRect.anchoredPosition = new Vector2(0f, 36f);
        okRect.sizeDelta = new Vector2(320f, 100f);
        okGo.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.85f, 1f);
        loginAlertOkButton = okGo.GetComponent<Button>();

        var okLabelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        okLabelGo.transform.SetParent(okGo.transform, false);
        var okLabelRect = okLabelGo.GetComponent<RectTransform>();
        okLabelRect.anchorMin = Vector2.zero;
        okLabelRect.anchorMax = Vector2.one;
        okLabelRect.offsetMin = Vector2.zero;
        okLabelRect.offsetMax = Vector2.zero;
        var okLabel = okLabelGo.GetComponent<Text>();
        okLabel.font = font;
        okLabel.fontSize = 40;
        okLabel.alignment = TextAnchor.MiddleCenter;
        okLabel.color = Color.white;
        okLabel.text = "확인";
        okLabel.raycastTarget = false;

        loginAlertOkButton.onClick.AddListener(HideLoginAlert);
        loginAlertPanel.SetActive(false);
    }

    Font GetUiFont() {
        if (InputField != null && InputField.textComponent != null && InputField.textComponent.font != null)
            return InputField.textComponent.font;
        if (InputField != null && InputField.placeholder is Text placeholder && placeholder.font != null)
            return placeholder.font;
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    string FormatLoginError(string error) {
        if (string.IsNullOrWhiteSpace(error))
            return "로그인에 실패했습니다. ID와 비밀번호를 확인해 주세요.";
        if (error.IndexOf("CLEARTEXT", StringComparison.OrdinalIgnoreCase) >= 0 ||
            error.IndexOf("cleartext", StringComparison.OrdinalIgnoreCase) >= 0)
            return "서버 HTTP 연결이 차단되었습니다. 앱을 최신 버전으로 업데이트해 주세요.";
        if (error.IndexOf("Cannot connect", StringComparison.OrdinalIgnoreCase) >= 0 ||
            error.IndexOf("Connection refused", StringComparison.OrdinalIgnoreCase) >= 0 ||
            error.IndexOf("Network unreachable", StringComparison.OrdinalIgnoreCase) >= 0 ||
            error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
            return "서버에 연결할 수 없습니다. Wi-Fi 연결과 서버 주소를 확인해 주세요.";
        if (error.IndexOf(JsonRequest.AccountNotFoundError, StringComparison.OrdinalIgnoreCase) >= 0)
            return "아이디가 올바르지 않습니다.";
        if (error.IndexOf("Invalid credentials", StringComparison.OrdinalIgnoreCase) >= 0 ||
            error.IndexOf("401", StringComparison.OrdinalIgnoreCase) >= 0)
            return "비밀번호가 올바르지 않습니다.";
        return "로그인에 실패했습니다. ID와 비밀번호를 확인해 주세요.";
    }

    void ShowLoginAlert(string message) {
        EnsureAlertPopup();
        if (loginAlertText != null)
            loginAlertText.text = message;

        if (InputField != null) {
            InputField.DeactivateInputField();
            InputField.interactable = false;
        }
        if (passwordInputField != null) {
            passwordInputField.DeactivateInputField();
            passwordInputField.interactable = false;
        }

        loginAlertPanel.transform.SetAsLastSibling();
        loginAlertPanel.SetActive(true);
        Debug.LogWarning("[LoginForm] " + message);
    }

    void HideLoginAlert() {
        if (loginAlertPanel != null)
            loginAlertPanel.SetActive(false);
        SetLoginInteractable(true);
    }

    void SetLoginInteractable(bool enabled) {
        if (nextButton != null)
            nextButton.interactable = enabled;
        if (InputField != null)
            InputField.interactable = enabled;
        if (passwordInputField != null)
            passwordInputField.interactable = enabled;
    }

    public void OnEnable() {
        DestroyUnityKeyboardIfAny();
        DisableLoginRaycastBlockers();
        EnsureOpaqueLoginBackdrop();
        EnsurePasswordField();
        ConfigureInputFieldsForSystemKeyboard();
        HideLoginChrome();

        // Restore login UI position if a previous keyboard lift left it shifted.
        var loginUi = transform.Find("LoginUI") as RectTransform;
        if (loginUi != null)
            loginUi.anchoredPosition = new Vector2(0.0337f, -0.016f);
        if (nextButton != null)
            nextButton.gameObject.SetActive(true);

        if (InputField != null) {
            InputField.text = "";
            InputField.DeactivateInputField();
        }
        if (passwordInputField != null) {
            passwordInputField.text = "";
            passwordInputField.DeactivateInputField();
            passwordInputField.transform.SetAsLastSibling();
        }
        HideLoginAlert();
        isSubmitting = false;
        SetLoginInteractable(true);
        if (dm != null)
            dm.DanceTiming = false;
    }

    public void WriteResult(string msg) {
        // Always keep local CSV, and mirror to server when logged in.
        WriteResultLocal(msg);

        if (jr == null)
            jr = FindFirstObjectByType<JsonRequest>(FindObjectsInactive.Include);

        if (jr != null && jr.HasSession)
            jr.PostTrainingResultAsync(msg, (ok, err) => {
                if (!ok)
                    MBodyDiagLog.Warn("Login", $"PostTrainingResult failed: {err}");
            });
    }

    void WriteResultLocal(string msg) {
        if (string.IsNullOrEmpty(savePath))
            return;

        var dt = DateTime.Now.ToString("yy-MM-dd HH:mm:ss");
        using (var outStream = new StreamWriter(savePath, true)) {
            outStream.WriteLine(dt + "," + msg);
        }
    }

    public void UpdateResult() {
        if (dm != null) {
            playTimes = dm.playTimes;
            danceStar = dm.danceStar;
        }

        var bodyPlayTimes = GetBodyPlayTimes();
        var playerId = !string.IsNullOrWhiteSpace(ID)
            ? ID
            : (jr != null ? jr.ID : null);

        if (!string.IsNullOrWhiteSpace(playerId))
            PlayerCsvStore.TryUpdateProgress(playerId, playTimes, danceStar);

        if (jr == null)
            jr = FindFirstObjectByType<JsonRequest>(FindObjectsInactive.Include);

        if (jr != null && jr.HasSession) {
            jr.SaveMyProgressAsync(playTimes, bodyPlayTimes, danceStar, (ok, err) => {
                if (!ok)
                    MBodyDiagLog.Warn("Login", $"SaveMyProgress failed: {err}");
            });
        }
    }

    /// <summary>
    /// Pulls the latest playTimes/danceStar from the server and writes Players.csv.
    /// Used when entering dance training so admin portal edits unlock content on device.
    /// </summary>
    public void SyncProgressFromServer(Action<bool> onComplete = null) {
        StartCoroutine(SyncProgressFromServerCoroutine(onComplete));
    }

    public IEnumerator SyncProgressFromServerCoroutine(Action<bool> onComplete = null) {
        if (jr == null)
            jr = FindFirstObjectByType<JsonRequest>(FindObjectsInactive.Include);

        if (jr == null || !jr.HasSession) {
            MBodyDiagLog.Warn("Login", "SyncProgressFromServer skipped: no session");
            onComplete?.Invoke(false);
            yield break;
        }

        var progressOk = false;
        var progressError = "";
        PlayerProgressData progress = null;
        yield return jr.SyncMyProgress((ok, err, data) => {
            progressOk = ok;
            progressError = err;
            progress = data;
        });

        if (!progressOk || progress == null) {
            MBodyDiagLog.Warn("Login", $"SyncProgressFromServer failed: {progressError}");
            onComplete?.Invoke(false);
            yield break;
        }

        ApplyServerProgress(progress);
        onComplete?.Invoke(true);
    }

    public void ApplyServerProgress(PlayerProgressData progress) {
        if (progress == null)
            return;

        var playerId = !string.IsNullOrWhiteSpace(ID)
            ? ID
            : (jr != null ? jr.ID : null);
        if (string.IsNullOrWhiteSpace(playerId))
            return;

        playerId = playerId.Trim();
        ID = playerId;
        if (progress.userAccountSeq > 0)
            idx = progress.userAccountSeq;

        playTimes = progress.playTimes;
        danceStar = string.IsNullOrWhiteSpace(progress.danceStar)
            ? "FFFFFF-FFFFFFF"
            : progress.danceStar;

        if (dm != null) {
            dm.ID = playerId;
            dm.playTimes = playTimes;
            dm.danceStar = danceStar;
        }

        ApplyBodyPlayTimes(progress.bodyPlayTimes);
        PlayerCsvStore.TryUpdateProgress(playerId, playTimes, danceStar);
        savePath = Path.Combine(MBodyPaths.DataRoot, "saveData_" + playerId + ".csv");
        MBodyDiagLog.Step("Login",
            $"Applied server progress id={playerId} playTimes={playTimes} danceStar={danceStar}");
    }
}
