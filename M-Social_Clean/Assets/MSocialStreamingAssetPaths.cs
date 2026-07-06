using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// StreamingAssets 경로 규칙:
/// - 에디터/디스크: Assets/StreamingAssets/{relative}
/// - APK 내부: assets/{relative}  (Unity Application.streamingAssetsPath + "/" + relative)
/// - movie_index.json 항목 예: Movie/Movie/Stage1/17-32/s1_019.mp4
/// </summary>
public static class MSocialStreamingAssetPaths
{
    public static string SanitizeRelativePath(string assetsRelativePath)
    {
        if (string.IsNullOrEmpty(assetsRelativePath))
            return assetsRelativePath;

        var rel = assetsRelativePath.Replace('\\', '/');
        while (rel.Contains("//", StringComparison.Ordinal))
            rel = rel.Replace("//", "/", StringComparison.Ordinal);
        return rel;
    }

    public static string ToStreamingAssetsUrl(string relativePath)
    {
        var rel = SanitizeRelativePath(relativePath);
        var basePath = Application.streamingAssetsPath;
        if (!basePath.EndsWith("/"))
            basePath += "/";
        return basePath + rel;
    }

    public static string ToEditorDiskPath(string relativePath)
    {
        var rel = SanitizeRelativePath(relativePath);
        return Path.Combine(Application.dataPath, "StreamingAssets", rel.Replace('/', Path.DirectorySeparatorChar));
    }

    public static string ToApkAssetPath(string relativePath)
    {
        return "assets/" + SanitizeRelativePath(relativePath);
    }

    public static string GetCacheFileName(string relativePath)
    {
        var rel = SanitizeRelativePath(relativePath);
        var fileName = Path.GetFileName(rel);
        if (string.IsNullOrEmpty(fileName))
            return "video.mp4";
        // Android file:// + VideoPlayer: non-ASCII cache names often fail first Prepare() until fallback URL.
        if (IsAsciiFileName(fileName))
            return fileName;
        return "cache_" + HashHex(rel) + ".mp4";
    }

    static bool IsAsciiFileName(string fileName)
    {
        foreach (var c in fileName)
        {
            if (c > 127)
                return false;
        }
        return true;
    }

    static string HashHex(string text)
    {
        using (var md5 = MD5.Create())
        {
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    public static string GetCacheFilePath(string relativePath)
    {
        return Path.Combine(Application.persistentDataPath, "Movies", GetCacheFileName(relativePath));
    }

    public static List<string> BuildPlayCandidates(string assetsMoviePath)
    {
        var list = new List<string>();
        void Add(string path)
        {
            var rel = SanitizeRelativePath(path);
            if (!string.IsNullOrEmpty(rel) && !list.Contains(rel))
                list.Add(rel);
        }

        Add(assetsMoviePath);

        var norm = SanitizeRelativePath(assetsMoviePath);
        if (norm.StartsWith("Movie/Movie/", StringComparison.OrdinalIgnoreCase))
            Add(norm.Substring("Movie/".Length));

        if (norm.StartsWith("Movie/Stage1/", StringComparison.OrdinalIgnoreCase)
            || norm.StartsWith("Movie/Stage2/", StringComparison.OrdinalIgnoreCase))
            Add("Movie/" + norm);

        return list;
    }

    public static string[] ToLocalFileUrlVariants(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath))
            return Array.Empty<string>();

        var fwd = absolutePath.Replace('\\', '/');
        var variants = new List<string>();

#if UNITY_ANDROID && !UNITY_EDITOR
        // VideoPlayer on Tab A8: raw path prepares most reliably; encoded Uri often times out first.
        variants.Add(fwd);
#endif

        var fileUri = fwd.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
            ? fwd
            : "file://" + fwd;
        if (!variants.Contains(fileUri))
            variants.Add(fileUri);

        var uri = new Uri(fwd).AbsoluteUri;
        if (!variants.Contains(uri))
            variants.Add(uri);

        return variants.ToArray();
    }

    public static IEnumerator ProbeReadable(string relativePath, Action<bool, long, string> onDone)
    {
        var rel = SanitizeRelativePath(relativePath);
        var url = ToStreamingAssetsUrl(rel);
        var apkPath = ToApkAssetPath(rel);
        var diskPath = ToEditorDiskPath(rel);

        MSocialStartupLog.Log("PathProbe rel=" + rel + " apk=" + apkPath + " url=" + url);

#if UNITY_EDITOR
        if (File.Exists(diskPath))
        {
            var len = new FileInfo(diskPath).Length;
            onDone(true, len, "editor-disk " + diskPath);
            yield break;
        }
        onDone(false, 0, "missing editor-disk " + diskPath);
        yield break;
#else
        using (var req = UnityWebRequest.Head(url))
        {
            req.timeout = 30;
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                long len = 0;
                var cl = req.GetResponseHeader("Content-Length");
                if (!string.IsNullOrEmpty(cl))
                    long.TryParse(cl, out len);
                onDone(true, len, "apk " + apkPath);
                yield break;
            }
        }

        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 60;
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success && req.downloadHandler?.data != null)
            {
                onDone(true, req.downloadHandler.data.Length, "apk-get " + apkPath);
                yield break;
            }

            onDone(false, 0, "apk-fail " + apkPath + " err=" + req.error);
        }
#endif
    }
}
