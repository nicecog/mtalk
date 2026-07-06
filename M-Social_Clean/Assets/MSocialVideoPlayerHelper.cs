using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class MSocialVideoPlayerHelper : MonoBehaviour
{
    const float PrepareTimeoutSeconds = 45f;
    const float FeedbackPrepareTimeoutSeconds = 12f;
    const float StartPlayTimeoutSeconds = 15f;

    public static IEnumerator PrepareUrl(VideoPlayer vp, string assetsMoviePath, RawImage targetImage = null)
    {
        yield return PrepareUrl(vp, assetsMoviePath, targetImage, PrepareTimeoutSeconds, false);
    }

    static IEnumerator PrepareUrl(VideoPlayer vp, string assetsMoviePath, RawImage targetImage, float timeoutSeconds, bool alertOnEachFailure)
    {
        if (vp == null || string.IsNullOrEmpty(assetsMoviePath))
            yield break;

        ConfigurePlayer(vp, targetImage);
        BindRenderTarget(vp, targetImage);

        string[] urls = null;
        yield return MSocialVideoCache.ResolvePlayUrls(assetsMoviePath, result => urls = result);
        if (urls == null || urls.Length == 0)
        {
            MSocialStartupLog.Log("Video no play urls: " + assetsMoviePath);
            MSocialOnScreenAlertLog.Push("Video no play urls:\n" + assetsMoviePath);
            yield break;
        }

        foreach (var url in urls)
        {
            if (string.IsNullOrEmpty(url))
                continue;

            if (!TrySetUrl(vp, url))
                continue;

            vp.Prepare();
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!vp.isPrepared && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (vp.isPrepared)
            {
                MSocialStartupLog.Log("Video prepared: " + url);
                if (targetImage != null)
                    targetImage.color = Color.white;
                yield break;
            }

            MSocialStartupLog.Log("Video prepare failed: " + url);
            if (alertOnEachFailure)
                MSocialOnScreenAlertLog.Push("Prepare failed:\n" + url);
        }

        MSocialStartupLog.Log("Video prepare all urls failed: " + assetsMoviePath);
        MSocialOnScreenAlertLog.Push("Prepare ALL failed:\n" + assetsMoviePath);
    }

    public static IEnumerator PlayPrepared(VideoPlayer vp)
    {
        if (vp == null)
            yield break;

        if (!vp.isPrepared)
        {
            MSocialStartupLog.Log("Video PlayPrepared skipped (not prepared)");
            MSocialOnScreenAlertLog.Push("Video PlayPrepared skipped (not prepared) url=" + (vp != null ? vp.url : ""));
            yield break;
        }

        vp.Play();
        float deadline = Time.realtimeSinceStartup + StartPlayTimeoutSeconds;
        while (!vp.isPlaying && Time.realtimeSinceStartup < deadline)
            yield return null;

        if (!vp.isPlaying)
        {
            MSocialStartupLog.Log("Video Play() did not start url=" + vp.url);
            MSocialOnScreenAlertLog.Push("Video Play() did not start url=" + vp.url);
            yield break;
        }

        MSocialStartupLog.Log("Video playing url=" + vp.url);
        while (vp.isPlaying)
            yield return null;

        MSocialStartupLog.Log("Video finished url=" + vp.url);
    }

    public static bool IsPrepared(VideoPlayer vp) => vp != null && vp.isPrepared;

    public static IEnumerator PrepareFeedbackClip(VideoPlayer vp, string streamingRelativePath, RawImage rawImage = null)
    {
        yield return PrepareUrl(vp, streamingRelativePath, rawImage, FeedbackPrepareTimeoutSeconds, false);
    }

    static bool TrySetUrl(VideoPlayer vp, string url)
    {
        if (vp.url == url && vp.isPrepared)
            return true;

        vp.source = VideoSource.Url;
        vp.clip = null;
        vp.url = url;
        vp.playOnAwake = false;
        vp.waitForFirstFrame = !MSocialDeviceProfile.IsLowMemoryDevice;
        if (vp.isPlaying)
            vp.Stop();
        return true;
    }

    static void ConfigurePlayer(VideoPlayer vp, RawImage targetImage)
    {
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.aspectRatio = VideoAspectRatio.FitInside;
        vp.skipOnDrop = true;

        var audio = vp.GetComponent<AudioSource>();
        if (audio == null)
            audio = vp.gameObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;

        vp.audioOutputMode = VideoAudioOutputMode.Direct;
        vp.SetTargetAudioSource(0, audio);
    }

    static void BindRenderTarget(VideoPlayer vp, RawImage targetImage)
    {
        if (targetImage == null)
            return;

        if (targetImage.texture != null)
        {
            var oldRt = targetImage.texture as RenderTexture;
            targetImage.texture = null;
            if (vp.targetTexture == oldRt)
                vp.targetTexture = null;
            if (oldRt != null && MSocialDeviceProfile.IsRuntimeRenderTexture(oldRt))
            {
                oldRt.Release();
                Object.Destroy(oldRt);
            }
        }

        var rt = MSocialDeviceProfile.CreateVideoRenderTexture();
        vp.targetTexture = rt;
        targetImage.texture = rt;
        targetImage.color = Color.black;
        targetImage.enabled = true;
        targetImage.raycastTarget = false;
    }

    public static RawImage FindLinkedRawImage(VideoPlayer vp)
    {
        if (vp == null)
            return null;
        var raw = vp.GetComponent<RawImage>();
        if (raw != null)
            return raw;
        if (vp.targetTexture != null)
        {
            foreach (var child in vp.GetComponentsInChildren<RawImage>(true))
            {
                if (child.texture == vp.targetTexture)
                    return child;
            }
        }
        return vp.GetComponentInChildren<RawImage>(true);
    }

    public static void ReleaseTarget(VideoPlayer vp, RawImage targetImage)
    {
        if (vp != null)
            vp.Stop();

        if (targetImage != null && targetImage.texture != null)
        {
            var rt = targetImage.texture as RenderTexture;
            targetImage.texture = null;
            if (vp != null)
                vp.targetTexture = null;
            if (rt != null && MSocialDeviceProfile.IsRuntimeRenderTexture(rt))
            {
                rt.Release();
                Object.Destroy(rt);
            }
        }
    }

    public static void ReleaseFeedbackClip(VideoPlayer vp, RawImage rawImage = null)
    {
        rawImage = rawImage != null ? rawImage : FindLinkedRawImage(vp);
        ReleaseTarget(vp, rawImage);
    }
}
