using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로그인 화면 알림 — Android Toast + 화면 Text.
/// </summary>
public static class MFaceLoginAlert
{
    public static void Show(string message, Text onScreenText = null, Color? color = null)
    {
        if (string.IsNullOrEmpty(message))
            return;

        Debug.Log("[MFaceLoginAlert] " + message);
        ShowAndroidToast(message);

        if (onScreenText != null)
        {
            onScreenText.gameObject.SetActive(true);
            onScreenText.text = message;
            if (color.HasValue)
                onScreenText.color = color.Value;
        }
    }

    static void ShowAndroidToast(string message)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using var toastClass = new AndroidJavaClass("android.widget.Toast");
            const int lengthLong = 1;
            using var toast = toastClass.CallStatic<AndroidJavaObject>("makeText", activity, message, lengthLong);
            toast.Call("show");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[MFaceLoginAlert] Toast failed: " + ex.Message);
        }
#endif
    }
}
