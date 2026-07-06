using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class MSocialVideoPaths
{
    public static string ToStreamingUrl(string assetsRelativePath)
    {
        var urls = GetPlayUrls(assetsRelativePath);
        return urls.Count > 0 ? urls[0] : string.Empty;
    }

    public static List<string> GetPlayUrls(string assetsRelativePath)
    {
        var urls = new List<string>();
        if (string.IsNullOrEmpty(assetsRelativePath))
            return urls;

        var rel = NormalizeRelativePath(assetsRelativePath);
        var basePath = Application.streamingAssetsPath;
        if (!basePath.EndsWith("/"))
            basePath += "/";

        var raw = basePath + rel;
        urls.Add(raw);

#if UNITY_ANDROID && !UNITY_EDITOR
        var encoded = EncodeAndroidStreamingUrl(basePath, rel);
        if (!string.IsNullOrEmpty(encoded) && encoded != raw)
            urls.Add(encoded);
#endif
        return urls;
    }

    static string NormalizeRelativePath(string assetsRelativePath)
    {
        var rel = assetsRelativePath.Replace('\\', '/');
        if (rel.StartsWith("Movie/", StringComparison.OrdinalIgnoreCase)
            || rel.StartsWith("Animations/", StringComparison.OrdinalIgnoreCase))
            return rel;
        return "Movie/" + rel;
    }

    static string EncodeAndroidStreamingUrl(string basePath, string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return basePath;

        var bang = basePath.IndexOf("!/", StringComparison.Ordinal);
        if (bang < 0)
            return basePath + relativePath;

        var sb = new StringBuilder(basePath.Substring(0, bang + 2));
        var segments = relativePath.Split('/');
        for (int i = 0; i < segments.Length; i++)
        {
            if (string.IsNullOrEmpty(segments[i]))
                continue;
            sb.Append(EncodePathSegment(segments[i]));
            if (i < segments.Length - 1)
                sb.Append('/');
        }
        return sb.ToString();
    }

    static string EncodePathSegment(string segment)
    {
        var bytes = Encoding.UTF8.GetBytes(segment);
        var sb = new StringBuilder(bytes.Length * 3);
        foreach (byte b in bytes)
        {
            if ((b >= 'a' && b <= 'z') || (b >= 'A' && b <= 'Z') || (b >= '0' && b <= '9')
                || b == '-' || b == '_' || b == '.' || b == '~')
                sb.Append((char)b);
            else
                sb.Append('%').Append(b.ToString("X2"));
        }
        return sb.ToString();
    }
}
