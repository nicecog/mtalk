using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Android: APK StreamingAssets → persistentDataPath 복사 후 file:// URL만 VideoPlayer에 전달.
/// </summary>
public static class MSocialVideoCache
{
    static string CacheDir => Path.Combine(Application.persistentDataPath, "Movies");

    public static IEnumerator ResolvePlayUrls(string assetsMoviePath, Action<string[]> onDone)
    {
        var urls = new List<string>();
        if (string.IsNullOrEmpty(assetsMoviePath))
        {
            onDone(urls.ToArray());
            yield break;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        foreach (var rel in MSocialStreamingAssetPaths.BuildPlayCandidates(assetsMoviePath))
        {
            var cachePath = MSocialStreamingAssetPaths.GetCacheFilePath(rel);
            if (File.Exists(cachePath) && new FileInfo(cachePath).Length > 1024)
            {
                AddLocalUrls(urls, cachePath);
                MSocialStartupLog.Log("VideoCache hit " + cachePath);
                onDone(urls.ToArray());
                yield break;
            }

            var sourceUrl = MSocialStreamingAssetPaths.ToStreamingAssetsUrl(rel);
            var apkPath = MSocialStreamingAssetPaths.ToApkAssetPath(rel);
            MSocialStartupLog.Log("VideoCache loading apk=" + apkPath + " url=" + sourceUrl);

            using (var req = UnityWebRequest.Get(sourceUrl))
            {
                req.timeout = 180;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success
                    && req.downloadHandler?.data != null
                    && req.downloadHandler.data.Length > 1024)
                {
                    Directory.CreateDirectory(CacheDir);
                    File.WriteAllBytes(cachePath, req.downloadHandler.data);
                    AddLocalUrls(urls, cachePath);
                    MSocialStartupLog.Log("VideoCache saved " + cachePath + " bytes=" + req.downloadHandler.data.Length);
                    onDone(urls.ToArray());
                    yield break;
                }

                MSocialStartupLog.Log("VideoCache failed apk=" + apkPath + " err=" + req.error);
                MSocialOnScreenAlertLog.Push("VideoCache fail\n" + apkPath + "\n" + req.error);
            }
        }

        var failMsg = "VideoCache ALL failed\n" + assetsMoviePath;
        MSocialStartupLog.Log(failMsg);
        MSocialOnScreenAlertLog.Push(failMsg);
        onDone(urls.ToArray());
        yield break;
#else
        foreach (var url in MSocialVideoPaths.GetPlayUrls(assetsMoviePath))
        {
            if (!string.IsNullOrEmpty(url) && !urls.Contains(url))
                urls.Add(url);
        }

        onDone(urls.ToArray());
#endif
    }

    static void AddLocalUrls(List<string> urls, string cachePath)
    {
        foreach (var url in MSocialStreamingAssetPaths.ToLocalFileUrlVariants(cachePath))
        {
            if (!string.IsNullOrEmpty(url) && !urls.Contains(url))
                urls.Add(url);
        }
    }
}
