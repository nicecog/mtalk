using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using NatML.Vision;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카메라 캡처 → 얼굴 크롭(BlazeFace) → FER+ 로컬 표정 분석.
/// </summary>
public static class MFaceLocalEmotionAnalyzer
{
    const float FaceCropPadding = 0.15f;

    static FerPlusLocalPredictor ferPlus;
    static BlazeFacePredictor blazeFace;
    static bool initStarted;
    static bool initDone;
    static string initError;

    public static bool IsReady => initDone && ferPlus != null;
    public static string LastInitError => initError;
    public static bool UsesBlazeFace => blazeFace != null;

    public static IEnumerator EnsureInitialized()
    {
        if (initDone || ferPlus != null)
            yield break;
        if (initStarted)
        {
            while (!initDone && string.IsNullOrEmpty(initError))
                yield return null;
            yield break;
        }

        initStarted = true;
        string modelPath = null;
        string bootstrapError = null;

        yield return MFaceModelBootstrap.EnsureAndroidModel(
            p => modelPath = p,
            e => bootstrapError = e);

        if (!string.IsNullOrEmpty(bootstrapError))
        {
            initError = bootstrapError;
            initDone = true;
            MFaceAndroidLog.Error("Model bootstrap failed: " + bootstrapError);
            yield break;
        }

        Task<FerPlusLocalPredictor> ferTask = null;
        Task<BlazeFacePredictor> faceTask = null;

        try
        {
            ferTask = FerPlusLocalPredictor.CreateFromPath(modelPath);
            faceTask = BlazeFacePredictor.Create(
                minScore: 0.55f,
                maxIoU: 0.45f,
                accessKey: null);
        }
        catch (Exception ex)
        {
            initError = ex.Message;
            initDone = true;
            MFaceAndroidLog.Error("FER+ init failed: " + ex.Message);
            yield break;
        }

        while ((ferTask != null && !ferTask.IsCompleted) || (faceTask != null && !faceTask.IsCompleted))
            yield return null;

        if (ferTask.IsFaulted)
        {
            initError = ferTask.Exception?.GetBaseException().Message ?? "FER+ init failed";
            initDone = true;
            MFaceAndroidLog.Error(initError);
            yield break;
        }

        ferPlus = ferTask.Result;

        if (faceTask != null)
        {
            if (faceTask.IsCompletedSuccessfully)
                blazeFace = faceTask.Result;
            else
                MFaceAndroidLog.Warn("BlazeFace unavailable, using center crop: " +
                    (faceTask.Exception?.GetBaseException().Message ?? "init failed"));
        }

        initDone = true;
        MFaceAndroidLog.Info("Ready. BlazeFace=" + (blazeFace != null));
    }

    public static IEnumerator CaptureFrame(RawImage cameraViewImage, Action<Texture2D> onCaptured)
    {
        yield return new WaitForEndOfFrame();
        onCaptured?.Invoke(CaptureFrameImmediate(cameraViewImage));
    }

    public static Texture2D CaptureFrameImmediate(RawImage cameraViewImage)
    {
        var texture = cameraViewImage.texture;
        int width = texture.width;
        int height = texture.height;
        var current = RenderTexture.active;
        var copied = new RenderTexture(width, height, 0);
        Graphics.Blit(texture, copied);
        RenderTexture.active = copied;
        var texture2D = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture2D.Apply();
        RenderTexture.active = current;
        copied.Release();
        return texture2D;
    }

    public static IEnumerator AnalyzeFile(
        string imagePath,
        int correctType,
        float thr,
        Action<int, Emotion, string> onComplete)
    {
        Texture2D tex = null;
        try
        {
            byte[] bytes = File.ReadAllBytes(imagePath);
            tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!tex.LoadImage(bytes))
            {
                UnityEngine.Object.Destroy(tex);
                onComplete?.Invoke(-1, null, "Failed to load image");
                yield break;
            }
        }
        catch (Exception ex)
        {
            onComplete?.Invoke(-1, null, ex.Message);
            yield break;
        }

        yield return AnalyzeTexture(tex, correctType, thr, onComplete);
        if (tex != null)
            UnityEngine.Object.Destroy(tex);
    }

    public static IEnumerator AnalyzeTexture(
        Texture2D texture,
        int correctType,
        float thr,
        Action<int, Emotion, string> onComplete)
    {
        yield return EnsureInitialized();
        if (!IsReady)
        {
            onComplete?.Invoke(-1, null, initError ?? "Local ML not initialized");
            yield break;
        }

        if (texture == null)
        {
            onComplete?.Invoke(-1, null, "Capture texture is null");
            yield break;
        }

        try
        {
            var gray = ExtractFaceGray64(texture);
            float[] logits = ferPlus.Predict(gray);
            var fer = MFaceEmotionMapper.FromLogits(logits);
            var em = MFaceEmotionMapper.ToLegacyEmotion(fer);
            int emotion = MFaceEmotionMapper.ClassifyMFace(em, correctType, thr);
            string emotionString = MFaceEmotionMapper.ToServerEmotionString(em);

            MFaceAndroidLog.Info(emotionString + " => type " + emotion);
            onComplete?.Invoke(emotion, em, emotionString);
        }
        catch (Exception ex)
        {
            MFaceAndroidLog.Error("Analyze failed: " + ex);
            onComplete?.Invoke(-1, null, ex.Message);
        }
    }

    public static float[] ExtractFaceGray64(Texture2D source)
    {
        Rect faceRect = DetectFaceRect(source);
        return CropResizeGrayscale(source, faceRect, 64);
    }

    static Rect DetectFaceRect(Texture2D source)
    {
        if (blazeFace != null)
        {
            try
            {
                var faces = blazeFace.Predict(new NatML.Features.MLImageFeature(source));
                if (faces != null && faces.Length > 0)
                {
                    var best = faces[0];
                    float w = source.width;
                    float h = source.height;
                    return ExpandRect(new Rect(
                        best.rect.x * w,
                        best.rect.y * h,
                        best.rect.width * w,
                        best.rect.height * h), source.width, source.height, FaceCropPadding);
                }
            }
            catch (Exception ex)
            {
                MFaceAndroidLog.Warn("BlazeFace predict failed: " + ex.Message);
            }
        }

        float side = Mathf.Min(source.width, source.height) * 0.72f;
        return ExpandRect(new Rect(
            (source.width - side) * 0.5f,
            (source.height - side) * 0.38f,
            side,
            side), source.width, source.height, FaceCropPadding);
    }

    static Rect ExpandRect(Rect rect, int imageWidth, int imageHeight, float paddingRatio)
    {
        float padX = rect.width * paddingRatio;
        float padY = rect.height * paddingRatio;
        float x = Mathf.Clamp(rect.x - padX, 0f, imageWidth - 1);
        float y = Mathf.Clamp(rect.y - padY, 0f, imageHeight - 1);
        float right = Mathf.Clamp(rect.xMax + padX, 1f, imageWidth);
        float bottom = Mathf.Clamp(rect.yMax + padY, 1f, imageHeight);
        return new Rect(x, y, right - x, bottom - y);
    }

    static float[] CropResizeGrayscale(Texture2D src, Rect pixelRect, int size)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(pixelRect.x), 0, src.width - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(pixelRect.y), 0, src.height - 1);
        int w = Mathf.Clamp(Mathf.RoundToInt(pixelRect.width), 1, src.width - x);
        int h = Mathf.Clamp(Mathf.RoundToInt(pixelRect.height), 1, src.height - y);

        var cropped = new Texture2D(w, h, TextureFormat.RGB24, false);
        cropped.SetPixels(src.GetPixels(x, y, w, h));
        cropped.Apply();

        var scaled = ScaleTexture(cropped, size, size);
        if (cropped != scaled)
            UnityEngine.Object.Destroy(cropped);

        var pixels = scaled.GetPixels();
        var gray = new float[size * size];
        for (int i = 0; i < gray.Length; i++)
        {
            var c = pixels[i];
            gray[i] = (c.r * 0.299f + c.g * 0.587f + c.b * 0.114f) * 255f;
        }

        UnityEngine.Object.Destroy(scaled);
        return gray;
    }

    static Texture2D ScaleTexture(Texture2D src, int targetW, int targetH)
    {
        var rt = RenderTexture.GetTemporary(targetW, targetH);
        Graphics.Blit(src, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var dst = new Texture2D(targetW, targetH, TextureFormat.RGB24, false);
        dst.ReadPixels(new Rect(0, 0, targetW, targetH), 0, 0);
        dst.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return dst;
    }

    public static void Reset()
    {
        ferPlus?.Dispose();
        blazeFace?.Dispose();
        ferPlus = null;
        blazeFace = null;
        initStarted = false;
        initDone = false;
        initError = null;
    }
}
