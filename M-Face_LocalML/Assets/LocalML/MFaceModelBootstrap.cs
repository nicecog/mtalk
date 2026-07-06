using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Android StreamingAssets(JAR)에서 FER+ TFLite를 persistentDataPath로 복사.
/// </summary>
public static class MFaceModelBootstrap
{
    const string RelativeModelPath = "ML/emotion-ferplus-8.tflite";
    const string CachedFileName = "emotion-ferplus-8.tflite";

    public static string CachedModelPath =>
        Path.Combine(Application.persistentDataPath, CachedFileName);

    public static IEnumerator EnsureAndroidModel(Action<string> onReady, Action<string> onError)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (File.Exists(CachedModelPath))
        {
            onReady?.Invoke(CachedModelPath);
            yield break;
        }

        string src = Path.Combine(Application.streamingAssetsPath, RelativeModelPath);
        using var req = UnityWebRequest.Get(src);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke("Failed to load model from StreamingAssets: " + req.error);
            yield break;
        }

        try
        {
            File.WriteAllBytes(CachedModelPath, req.downloadHandler.data);
            onReady?.Invoke(CachedModelPath);
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
#else
        onReady?.Invoke(FerPlusLocalPredictor.ResolveModelPath());
        yield break;
#endif
    }
}
