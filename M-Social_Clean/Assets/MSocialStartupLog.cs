using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 실기기 크래시 분석용 — persistentDataPath/startup.log 에 단계 기록.
/// </summary>
public static class MSocialStartupLog
{
    static string LogPath => Path.Combine(Application.persistentDataPath, "startup.log");

    public static void Log(string message)
    {
        var line = DateTime.Now.ToString("HH:mm:ss.fff") + " " + message;
        Debug.Log("[MSocialStartup] " + message);
        try
        {
            File.AppendAllText(LogPath, line + Environment.NewLine);
        }
        catch
        {
            // ignore file IO on restricted storage
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void MarkBoot()
    {
        try
        {
            File.WriteAllText(LogPath, "=== MSocial boot " + DateTime.Now + " ===" + Environment.NewLine);
        }
        catch
        {
            // ignore
        }
        Log("BeforeSceneLoad Unity=" + Application.unityVersion + " RAM=" + SystemInfo.systemMemorySize + "MB");
    }
}
