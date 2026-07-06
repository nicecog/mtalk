using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public static class VideoThumbnailGenerator
{
    const int ThumbnailWidth = 854;
    const int ThumbnailHeight = 480;
    const float WaitTimeoutSeconds = 8f;

    static Texture2D scratchTexture;

    public static IEnumerator CaptureButtonThumbnail(
        VideoPlayer vp,
        RenderTexture targetTexture,
        Shader materialShader,
        Button button,
        string videoPath,
        float captureTime = 10f)
    {
        if (vp == null || button == null || string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath))
        {
            MBodyDiagLog.Warn("VideoThumb", $"Skip thumbnail; missing player/button/file path={videoPath}");
            if (button != null)
                button.interactable = true;
            yield break;
        }

        vp.Stop();
        vp.url = videoPath;
        vp.targetTexture = targetTexture;
        vp.Play();

        var wait = WaitTimeoutSeconds;
        while (!vp.isPlaying && wait > 0f)
        {
            wait -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!vp.isPlaying)
        {
            MBodyDiagLog.Warn("VideoThumb", $"Play timeout for {Path.GetFileName(videoPath)}");
            vp.Stop();
            button.interactable = true;
            yield break;
        }

        vp.time = captureTime;
        var frame = vp.frame;
        wait = WaitTimeoutSeconds;
        while (frame == vp.frame && wait > 0f)
        {
            wait -= Time.unscaledDeltaTime;
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.1f);
        button.image.sprite = null;
        button.image.material = new Material(materialShader);

        if (scratchTexture == null || scratchTexture.width != ThumbnailWidth || scratchTexture.height != ThumbnailHeight)
        {
            if (scratchTexture != null)
                Object.Destroy(scratchTexture);
            scratchTexture = new Texture2D(ThumbnailWidth, ThumbnailHeight, TextureFormat.RGB24, false);
        }

        if (vp.targetTexture == null)
        {
            MBodyDiagLog.Warn("VideoThumb", $"No target texture for {Path.GetFileName(videoPath)}");
            vp.Stop();
            button.interactable = true;
            yield break;
        }

        var prevActive = RenderTexture.active;
        RenderTexture.active = vp.targetTexture;
        var srcW = vp.targetTexture.width;
        var srcH = vp.targetTexture.height;
        var x = Mathf.Max(0, (srcW - ThumbnailWidth) / 2);
        var y = Mathf.Max(0, (srcH - ThumbnailHeight) / 2);
        var readW = Mathf.Min(ThumbnailWidth, srcW);
        var readH = Mathf.Min(ThumbnailHeight, srcH);
        scratchTexture.ReadPixels(new Rect(x, y, readW, readH), 0, 0);
        scratchTexture.Apply();
        RenderTexture.active = prevActive;

        button.image.material.mainTexture = scratchTexture;
        vp.targetTexture = null;
        vp.Pause();
        button.interactable = true;
    }
}
