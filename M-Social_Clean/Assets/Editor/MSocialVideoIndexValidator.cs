using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class MSocialVideoIndexValidator
{
    [Serializable]
    public class MovieIndexData
    {
        public string[] stage1;
        public string[] stage2;
        public string[] intro;
    }

    [MenuItem("MSocial/Validate Video Index")]
    public static void ValidateFromMenu()
    {
        var ok = Validate(out var report);
        Debug.Log(report);
        if (!ok)
            Debug.LogError("MSocial video index validation failed.");
    }

    public static bool Validate(out string report)
    {
        var sb = new StringBuilder();
        var ok = true;
        var root = Application.dataPath;
        var indexPath = Path.Combine(root, "StreamingAssets", "Movie", "movie_index.json");
        if (!File.Exists(indexPath))
        {
            report = "movie_index.json missing";
            return false;
        }

        var json = File.ReadAllText(indexPath);
        var data = JsonUtility.FromJson<MovieIndexData>(json);
        ok &= CheckList(sb, "stage1", data.stage1, 32, root);
        ok &= CheckList(sb, "stage2", data.stage2, 32, root);
        ok &= CheckList(sb, "intro", data.intro, 4, root);

        foreach (var sp in UnityEngine.Object.FindObjectsByType<ScenePlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sp.VideoCodes == null || sp.titles == null)
                continue;
            if (sp.VideoCodes.Length != 32 || sp.titles.Length != 32)
            {
                sb.AppendLine("ScenePlayer array length mismatch: codes=" + sp.VideoCodes.Length + " titles=" + sp.titles.Length);
                ok = false;
            }

            for (int cat = 1; cat <= 6; cat++)
            {
                var indices = Enumerable.Range(0, sp.VideoCodes.Length)
                    .Where(i => sp.VideoCodes[i] / 100 == cat)
                    .ToList();
                for (int n = 0; n < indices.Count; n++)
                {
                    int expectedCode = cat * 100 + (n + 1);
                    int idx = indices[n];
                    if (sp.VideoCodes[idx] != expectedCode)
                    {
                        sb.AppendLine("Category " + cat + " slot " + n + " idx=" + idx + " code=" + sp.VideoCodes[idx] + " expected=" + expectedCode);
                        ok = false;
                    }
                }
            }
        }

        report = sb.Length > 0 ? sb.ToString() : "Video index OK";
        return ok;
    }

    static bool CheckList(StringBuilder sb, string label, string[] paths, int expected, string root)
    {
        if (paths == null || paths.Length != expected)
        {
            sb.AppendLine(label + " count=" + (paths?.Length ?? 0) + " expected=" + expected);
            return false;
        }

        int missing = 0;
        foreach (var rel in paths)
        {
            var file = MSocialStreamingAssetPaths.ToEditorDiskPath(rel);
            if (!File.Exists(file))
            {
                missing++;
                if (missing <= 5)
                    sb.AppendLine("Missing " + label + ": " + rel);
            }
        }

        if (missing > 0)
        {
            sb.AppendLine(label + " missing files: " + missing);
            return false;
        }

        sb.AppendLine(label + ": " + paths.Length + " files present");
        return true;
    }
}
