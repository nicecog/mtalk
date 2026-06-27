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
        EnsurePasswordField();
        EnsureAlertPopup();
        ConfigureInputFieldsForTouch();
    }

    void Update() {
        if (mask != null)
            mask.enabled = CanSubmit();
    }

    void EnsurePasswordField() {
        if (passwordInputField != null || InputField == null)
            return;

        var clone = Instantiate(InputField, InputField.transform.parent);
        clone.name = "PasswordInputField";
        var idRect = InputField.GetComponent<RectTransform>();
        var passwordRect = clone.GetComponent<RectTransform>();
        passwordRect.anchoredPosition = idRect.anchoredPosition + new Vector2(0f, passwordFieldOffsetY);

        passwordInputField = clone.GetComponent<InputField>();
        passwordInputField.text = "";
        passwordInputField.contentType = InputField.ContentType.Password;
        passwordInputField.inputType = InputField.InputType.Password;
        ConfigureField(passwordInputField);

        var placeholder = passwordInputField.placeholder as Text;
        if (placeholder != null)
            placeholder.text = "Password";
    }

    void ConfigureInputFieldsForTouch() {
        if (InputField != null)
            ConfigureField(InputField);
        if (passwordInputField != null)
            ConfigureField(passwordInputField);
    }

    static void ConfigureField(InputField field) {
        field.shouldHideMobileInput = false;
        field.readOnly = false;
        field.interactable = true;

        var text = field.textComponent;
        if (text != null)
            text.raycastTarget = false;

        if (field.placeholder is Graphic placeholder)
            placeholder.raycastTarget = false;
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

        if (!PlayerCsvStore.TryPrepareOnLogin(playerId, out var record)) {
            isSubmitting = false;
            SetLoginInteractable(true);
            ShowLoginAlert("로컬 플레이어 데이터를 저장하지 못했습니다.");
            yield break;
        }

        ID = record.id;
        idx = record.idx;
        playTimes = record.playTimes;
        danceStar = record.danceStar;
        savePath = record.savePath;
        dm.ID = playerId;
        dm.playTimes = playTimes;
        dm.danceStar = danceStar;

        isSubmitting = false;
        SetLoginInteractable(true);
        HideLoginAlert();
        MBodyDiagLog.Step("Login", $"Success id={playerId} -> navigate via ButtonFlow");
        bf.ClickNext();
    }

    void EnsureAlertPopup() {
        if (loginAlertPanel != null)
            return;

        var parent = InputField != null ? InputField.transform.parent : transform;
        var font = GetUiFont();

        loginAlertPanel = new GameObject("LoginAlertPanel", typeof(RectTransform));
        loginAlertPanel.transform.SetParent(parent, false);
        var panelRect = loginAlertPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var backdrop = loginAlertPanel.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.55f);
        backdrop.raycastTarget = true;

        var box = new GameObject("AlertBox", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(loginAlertPanel.transform, false);
        var boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(920f, 420f);
        box.GetComponent<Image>().color = Color.white;

        var messageGo = new GameObject("Message", typeof(RectTransform), typeof(Text));
        messageGo.transform.SetParent(box.transform, false);
        var messageRect = messageGo.GetComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0.05f, 0.35f);
        messageRect.anchorMax = new Vector2(0.95f, 0.95f);
        messageRect.offsetMin = Vector2.zero;
        messageRect.offsetMax = Vector2.zero;
        loginAlertText = messageGo.GetComponent<Text>();
        loginAlertText.font = font;
        loginAlertText.fontSize = 42;
        loginAlertText.alignment = TextAnchor.MiddleCenter;
        loginAlertText.color = Color.black;
        loginAlertText.horizontalOverflow = HorizontalWrapMode.Wrap;
        loginAlertText.verticalOverflow = VerticalWrapMode.Overflow;

        var okGo = new GameObject("OkButton", typeof(RectTransform), typeof(Image), typeof(Button));
        okGo.transform.SetParent(box.transform, false);
        var okRect = okGo.GetComponent<RectTransform>();
        okRect.anchorMin = okRect.anchorMax = new Vector2(0.5f, 0.5f);
        okRect.anchoredPosition = new Vector2(0f, -130f);
        okRect.sizeDelta = new Vector2(260f, 90f);
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
        okLabel.fontSize = 38;
        okLabel.alignment = TextAnchor.MiddleCenter;
        okLabel.color = Color.white;
        okLabel.text = "확인";

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
        loginAlertPanel.SetActive(true);
        Debug.LogWarning("[LoginForm] " + message);
    }

    void HideLoginAlert() {
        if (loginAlertPanel != null)
            loginAlertPanel.SetActive(false);
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
        DisableLoginRaycastBlockers();
        EnsureOpaqueLoginBackdrop();
        ConfigureInputFieldsForTouch();
        HideLoginChrome();
        if (InputField != null)
            InputField.text = "";
        if (passwordInputField != null)
            passwordInputField.text = "";
        HideLoginAlert();
        isSubmitting = false;
        SetLoginInteractable(true);
        if (dm != null)
            dm.DanceTiming = false;
    }

    public void WriteResult(string msg) {
        var dt = DateTime.Now.ToString("yy-MM-dd HH:mm:ss");
        using (var outStream = new StreamWriter(savePath, true)) {
            outStream.WriteLine(dt + "," + msg);
        }
    }

    public void UpdateResult() {
        playTimes = dm.playTimes;
        danceStar = dm.danceStar;
        PlayerCsvStore.TryUpdateProgress(ID, playTimes, danceStar);
    }
}
