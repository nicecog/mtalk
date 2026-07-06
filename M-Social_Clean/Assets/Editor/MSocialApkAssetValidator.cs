using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class MSocialApkAssetValidator
{
    [MenuItem("MSocial/Validate APK Movie Paths")]
    public static void ValidateApkFromMenu()
    {
        var apk = Path.Combine(Application.dataPath, "..", "Build", "Android", "MSocial-latest-stable.apk");
        var ok = ValidateApk(apk, out var report);
        Debug.Log(report);
        if (!ok)
            Debug.LogError("APK movie path validation failed.");
    }

    public static bool ValidateApk(string apkPath, out string report)
    {
        var sb = new StringBuilder();
        var ok = true;
        apkPath = Path.GetFullPath(apkPath);

        if (!File.Exists(apkPath))
        {
            report = "APK not found: " + apkPath;
            return false;
        }

        var indexPath = Path.Combine(Application.dataPath, "StreamingAssets", "Movie", "movie_index.json");
        var json = File.ReadAllText(indexPath);
        var data = JsonUtility.FromJson<MSocialVideoIndexValidator.MovieIndexData>(json);

        using (var zip = ZipFile.OpenRead(apkPath))
        {
            var names = zip.Entries.Select(e => e.FullName.Replace('\\', '/')).ToHashSet(StringComparer.OrdinalIgnoreCase);
            ok &= CheckList(sb, "stage1", data.stage1, names);
            ok &= CheckList(sb, "stage2", data.stage2, names);
        }

        report = sb.Length > 0 ? sb.ToString() : "APK paths OK";
        return ok;
    }

    static bool CheckList(StringBuilder sb, string label, string[] paths, System.Collections.Generic.HashSet<string> apkEntries)
    {
        if (paths == null)
        {
            sb.AppendLine(label + " null");
            return false;
        }

        int missing = 0;
        foreach (var rel in paths)
        {
            var apkPath = MSocialStreamingAssetPaths.ToApkAssetPath(rel);
            if (!apkEntries.Contains(apkPath))
            {
                missing++;
                if (missing <= 5)
                    sb.AppendLine("APK missing " + label + ": " + apkPath + " (index=" + rel + ")");
            }
        }

        if (missing > 0)
        {
            sb.AppendLine(label + " APK missing: " + missing);
            return false;
        }

        sb.AppendLine(label + ": " + paths.Length + " APK entries OK");
        return true;
    }
}
