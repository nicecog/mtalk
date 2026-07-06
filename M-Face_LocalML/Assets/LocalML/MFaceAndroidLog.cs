using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Android logcat (tag: MFaceLocalML) + Unity console. Works on release builds.
/// </summary>
public static class MFaceAndroidLog
{
    public const string Tag = "MFaceLocalML";

    const int AndroidLogInfo = 4;
    const int AndroidLogWarn = 5;
    const int AndroidLogError = 6;

#if UNITY_ANDROID && !UNITY_EDITOR
    [DllImport("log", EntryPoint = "__android_log_write")]
    static extern void AndroidLogWrite(int prio, string tag, string text);
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void LogStartup()
    {
        Info("M-Face scene loaded");
    }

    public static void Info(string message)
    {
        Debug.Log("[" + Tag + "] " + message);
        WriteAndroidLog(AndroidLogInfo, message);
    }

    public static void Warn(string message)
    {
        Debug.LogWarning("[" + Tag + "] " + message);
        WriteAndroidLog(AndroidLogWarn, message);
    }

    public static void Error(string message)
    {
        Debug.LogError("[" + Tag + "] " + message);
        WriteAndroidLog(AndroidLogError, message);
    }

    static void WriteAndroidLog(int prio, string message)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidLogWrite(prio, Tag, message);
#endif
    }
}
