using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Ensures UI VideoPlayer instances prepare, play, and fit the current screen on enable.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(VideoPlayer))]
public sealed class UiVideoPlayer : MonoBehaviour
{
    const float PrepareTimeoutSeconds = 12f;

    VideoPlayer vp;
    RenderTexture runtimeTexture;
    RectTransform displayRect;
    bool ownsTexture;
    string pagePath;

    void Awake()
    {
        vp = GetComponent<VideoPlayer>();
        vp.playOnAwake = false;
        displayRect = GetComponent<RectTransform>();
        pagePath = DescribePagePath();
        vp.errorReceived += OnVideoError;
        vp.prepareCompleted += OnPrepareCompleted;
        MBodyDiagLog.Step("UiVideo", $"Awake go='{name}' page={pagePath}");
    }

    void OnEnable()
    {
        if (!IsUnderActiveSceneFlowPage())
        {
            MBodyDiagLog.Warn("UiVideo", $"OnEnable skipped (inactive SceneFlow parent) go='{name}' page={pagePath}");
            return;
        }

        if (ScenePageVideoDriver.Instance != null)
        {
            MBodyDiagLog.Step("UiVideo", $"OnEnable defer to VideoDriver go='{name}' page={pagePath}");
            return;
        }

        MBodyDiagLog.Step("UiVideo", $"OnEnable go='{name}' page={pagePath} clip={DescribeClip()}");
        StartCoroutine(PlayWhenReady());
    }

    void OnDisable()
    {
        StopPlayback();
    }

    void OnDestroy()
    {
        if (vp != null)
        {
            vp.errorReceived -= OnVideoError;
            vp.prepareCompleted -= OnPrepareCompleted;
        }

        ReleaseTexture();
    }

    void OnVideoError(VideoPlayer source, string message)
    {
        MBodyDiagLog.Error("UiVideo", $"errorReceived go='{name}' page={pagePath} msg={message} clip={DescribeClip()}");
    }

    void OnPrepareCompleted(VideoPlayer source)
    {
        MBodyDiagLog.Step("UiVideo", $"prepareCompleted go='{name}' page={pagePath} size={source.width}x{source.height}");
    }

    public void PlayFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return;

        vp.source = VideoSource.Url;
        vp.url = url;
        if (isActiveAndEnabled)
            StartCoroutine(PlayWhenReady());
    }

    public void RestartPlayback()
    {
        if (!isActiveAndEnabled)
            return;

        StartCoroutine(PlayWhenReady());
    }

    IEnumerator PlayWhenReady()
    {
        if (!HasPlayableSource())
        {
            MBodyDiagLog.Error("UiVideo", $"No playable source go='{name}' page={pagePath}");
            yield break;
        }

        MBodyDiagLog.Step("UiVideo", $"Prepare start go='{name}' page={pagePath} clip={DescribeClip()}");

        ConfigureOutput();
        FitDisplay();

        vp.Prepare();
        var timeout = PrepareTimeoutSeconds;
        while (!vp.isPrepared && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!vp.isPrepared)
        {
            MBodyDiagLog.Error("UiVideo", $"Prepare timeout ({PrepareTimeoutSeconds}s) go='{name}' page={pagePath} clip={DescribeClip()}");
            yield break;
        }

        if (vp.source == VideoSource.Url)
        {
            ConfigureOutput();
            FitDisplay();
        }

        vp.Play();
        MBodyDiagLog.Step("UiVideo", $"Playing go='{name}' page={pagePath} source={DescribeSource()} rt={runtimeTexture?.width}x{runtimeTexture?.height} isPlaying={vp.isPlaying}");
    }

    bool HasPlayableSource()
    {
        if (vp.source == VideoSource.Url)
            return !string.IsNullOrEmpty(vp.url);

        return vp.clip != null;
    }

    string DescribeSource()
    {
        if (vp.source == VideoSource.Url)
            return vp.url;
        return vp.clip != null ? vp.clip.name : "(none)";
    }

    string DescribeClip()
    {
        if (vp.clip == null)
            return "null";

        return $"'{vp.clip.name}' {vp.clip.width}x{vp.clip.height} len={vp.clip.length:F1}s";
    }

    string DescribePagePath()
    {
        var current = transform;
        while (current != null)
        {
            if (current.CompareTag("SceneFlow"))
                return $"{current.name}#{current.gameObject.GetInstanceID()}";
            current = current.parent;
        }

        return "(no SceneFlow parent)";
    }

    void ConfigureOutput()
    {
        var (width, height) = ResolveTargetSize();
        EnsureRenderTexture(width, height);

        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetTexture = runtimeTexture;
        vp.aspectRatio = VideoAspectRatio.FitInside;

        BindDisplayTexture(runtimeTexture);
        MBodyDiagLog.Step("UiVideo", $"ConfigureOutput go='{name}' rt={width}x{height} image={(GetComponent<Image>() != null)} raw={(GetComponent<RawImage>() != null)}");
    }

    (int width, int height) ResolveTargetSize()
    {
        var screenW = Mathf.Max(320, Screen.width);
        var screenH = Mathf.Max(240, Screen.height);

        if (PerformanceManager.Instance != null && PerformanceManager.Instance.IsReady)
        {
            var cam = PerformanceManager.Instance.CameraResolution;
            screenW = Mathf.Min(screenW, cam.x);
            screenH = Mathf.Min(screenH, cam.y);
        }

        uint clipW = 0;
        uint clipH = 0;
        if (vp.clip != null)
        {
            clipW = vp.clip.width;
            clipH = vp.clip.height;
        }
        else if (vp.isPrepared)
        {
            clipW = (uint)vp.width;
            clipH = (uint)vp.height;
        }

        if (clipW > 0 && clipH > 0)
        {
            var scale = Mathf.Min(screenW / (float)clipW, screenH / (float)clipH, 1f);
            return (
                Mathf.Max(320, Mathf.RoundToInt(clipW * scale)),
                Mathf.Max(180, Mathf.RoundToInt(clipH * scale)));
        }

        var aspect = screenW / (float)screenH;
        if (aspect >= 16f / 9f)
            return (screenW, Mathf.RoundToInt(screenW * 9f / 16f));

        return (Mathf.RoundToInt(screenH * 16f / 10f), screenH);
    }

    void EnsureRenderTexture(int width, int height)
    {
        if (runtimeTexture != null && runtimeTexture.width == width && runtimeTexture.height == height)
            return;

        ReleaseTexture();
        runtimeTexture = new RenderTexture(width, height, 0, RenderTextureFormat.Default)
        {
            name = $"UiVideo_{name}",
            useMipMap = false,
            autoGenerateMips = false
        };
        runtimeTexture.Create();
        ownsTexture = true;
    }

    void BindDisplayTexture(Texture texture)
    {
        var raw = GetComponent<RawImage>();
        if (raw != null)
        {
            raw.texture = texture;
            raw.raycastTarget = false;
            return;
        }

        var image = GetComponent<Image>();
        if (image == null)
        {
            MBodyDiagLog.Warn("UiVideo", $"No Image/RawImage on '{name}'");
            return;
        }

        image.raycastTarget = false;

        if (image.material != null && image.material.HasProperty("_MainTex"))
        {
            image.material = Instantiate(image.material);
            image.material.mainTexture = texture;
            image.sprite = null;
            return;
        }

        image.sprite = null;
        MBodyDiagLog.Warn("UiVideo", $"Image on '{name}' has no _MainTex material; video may be invisible");
    }

    void FitDisplay()
    {
        if (displayRect == null)
            return;

        var parent = displayRect.parent as RectTransform;
        if (parent == null)
            return;

        var availW = parent.rect.width;
        var availH = parent.rect.height;
        if (availW <= 0f || availH <= 0f)
        {
            var canvas = parent.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var canvasRect = canvas.rootCanvas.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    availW = canvasRect.rect.width;
                    availH = canvasRect.rect.height;
                }
            }

            if (availW <= 0f)
                availW = Screen.width;
            if (availH <= 0f)
                availH = Screen.height;

            MBodyDiagLog.Warn("UiVideo", $"FitDisplay parent size was zero on '{name}'; using {availW}x{availH}");
        }

        float videoAspect = 16f / 9f;
        if (runtimeTexture != null && runtimeTexture.height > 0)
            videoAspect = runtimeTexture.width / (float)runtimeTexture.height;
        else if (vp.clip != null && vp.clip.height > 0)
            videoAspect = vp.clip.width / (float)vp.clip.height;

        float w;
        float h;
        var parentAspect = availW / availH;
        if (videoAspect > parentAspect)
        {
            w = availW;
            h = availW / videoAspect;
        }
        else
        {
            h = availH;
            w = availH * videoAspect;
        }

        displayRect.anchorMin = displayRect.anchorMax = new Vector2(0.5f, 0.5f);
        displayRect.pivot = new Vector2(0.5f, 0.5f);
        displayRect.anchoredPosition = Vector2.zero;
        displayRect.sizeDelta = new Vector2(w, h);
    }

    bool IsUnderActiveSceneFlowPage()
    {
        var current = transform;
        while (current != null)
        {
            if (current.CompareTag("SceneFlow"))
                return current.gameObject.activeInHierarchy;

            current = current.parent;
        }

        return false;
    }

    void StopPlayback()
    {
        if (vp == null)
            return;

        if (vp.isPlaying)
            vp.Stop();
    }

    void ReleaseTexture()
    {
        if (!ownsTexture || runtimeTexture == null)
            return;

        if (vp != null && vp.targetTexture == runtimeTexture)
            vp.targetTexture = null;

        runtimeTexture.Release();
        Destroy(runtimeTexture);
        runtimeTexture = null;
        ownsTexture = false;
    }
}
