using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// USB 연결이 안 될 때 화면에서 바로 원인 로그를 확인하기 위한 오버레이.
/// </summary>
public class MSocialOnScreenAlertLog : MonoBehaviour
{
    static MSocialOnScreenAlertLog _instance;
    static readonly List<string> _lines = new List<string>();
    const int MaxLines = 6;

    Text _text;
    Text _statusText;

    public static void Ensure(RectTransform parent = null)
    {
        if (_instance != null)
            return;

        var canvasGo = new GameObject("MSocialDebugAlertCanvas");
        Object.DontDestroyOnLoad(canvasGo);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        var logGo = new GameObject("LogText");
        logGo.transform.SetParent(canvasGo.transform, false);
        _instance = logGo.AddComponent<MSocialOnScreenAlertLog>();

        var logText = logGo.AddComponent<Text>();
        logText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        logText.fontSize = 22;
        logText.alignment = TextAnchor.UpperLeft;
        logText.color = new Color(1f, 0.3f, 0.3f, 1f);
        logText.raycastTarget = false;
        logText.horizontalOverflow = HorizontalWrapMode.Wrap;
        logText.verticalOverflow = VerticalWrapMode.Overflow;

        var logRt = logGo.GetComponent<RectTransform>();
        logRt.anchorMin = new Vector2(0, 1);
        logRt.anchorMax = new Vector2(1, 1);
        logRt.pivot = new Vector2(0.5f, 1f);
        logRt.anchoredPosition = new Vector2(0, -8);
        logRt.sizeDelta = new Vector2(-24, 320);

        var statusGo = new GameObject("StatusText");
        statusGo.transform.SetParent(canvasGo.transform, false);
        var statusText = statusGo.AddComponent<Text>();
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 30;
        statusText.fontStyle = FontStyle.Bold;
        statusText.alignment = TextAnchor.LowerCenter;
        statusText.color = Color.yellow;
        statusText.raycastTarget = false;
        statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
        statusText.verticalOverflow = VerticalWrapMode.Overflow;

        var statusRt = statusGo.GetComponent<RectTransform>();
        statusRt.anchorMin = new Vector2(0, 0);
        statusRt.anchorMax = new Vector2(1, 0);
        statusRt.pivot = new Vector2(0.5f, 0f);
        statusRt.anchoredPosition = new Vector2(0, 24);
        statusRt.sizeDelta = new Vector2(-24, 200);

        _instance._text = logText;
        _instance._statusText = statusText;
        _instance._text.gameObject.SetActive(true);
        _instance._statusText.gameObject.SetActive(true);
        _instance.Refresh();
    }

    public static void SetStatus(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;
        if (_instance == null)
            Ensure();
        if (_instance?._statusText != null)
            _instance._statusText.text = message;
    }

    public static void Push(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        if (_lines.Count >= MaxLines)
            _lines.RemoveAt(0);
        _lines.Add(message);

        if (_instance == null)
            Ensure();

        if (_instance != null)
            _instance.Refresh();

        SetStatus(message.Replace('\n', ' '));
        MSocialLoginAlert.Show(message);
    }

    public static void PushWhiteScreen(string context, string sceneCategory, string code, string path, bool prepared, bool playedProgressed)
    {
        var msg =
            "BLACK_SCREEN [" + context + "] cat=" + sceneCategory + " code=" + code + "\n" +
            path + "\nprepared=" + prepared + " play=" + playedProgressed;
        Push(msg);
    }

    void Refresh()
    {
        if (_text != null)
            _text.text = string.Join("\n", _lines.ToArray());
    }
}
