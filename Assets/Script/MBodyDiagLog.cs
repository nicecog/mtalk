using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Step-by-step diagnostics for scene flow and video playback.
/// Android: also writes to persistentDataPath/MBody/diag.log
/// </summary>
public static class MBodyDiagLog
{
    static string logPath;
    static bool initialized;

    public static void Step(string tag, string message)
    {
        var line = $"[{tag}] {message}";
        Debug.Log(line);
        Append(line);
    }

    public static void Warn(string tag, string message)
    {
        var line = $"[{tag}] WARN {message}";
        Debug.LogWarning(line);
        Append(line);
    }

    public static void Error(string tag, string message)
    {
        var line = $"[{tag}] ERROR {message}";
        Debug.LogError(line);
        Append(line);
    }

    static void Append(string line)
    {
        try
        {
            EnsurePath();
            File.AppendAllText(logPath, DateTime.Now.ToString("HH:mm:ss.fff") + " " + line + Environment.NewLine);
        }
        catch
        {
            // Ignore file IO failures; Unity console still has the message.
        }
    }

    static void EnsurePath()
    {
        if (initialized)
            return;

        var dir = Path.Combine(Application.persistentDataPath, "MBody");
        Directory.CreateDirectory(dir);
        logPath = Path.Combine(dir, "diag.log");
        initialized = true;
        File.AppendAllText(logPath, Environment.NewLine + $"===== session {DateTime.Now:yyyy-MM-dd HH:mm:ss} =====" + Environment.NewLine);
    }

    public static string LogFilePath
    {
        get
        {
            EnsurePath();
            return logPath;
        }
    }
}
