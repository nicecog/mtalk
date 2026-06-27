using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Explicit UI video playback for SceneFlow pages (Android-safe).
/// </summary>
public sealed class ScenePageVideoDriver : MonoBehaviour
{
    public static ScenePageVideoDriver Instance { get; private set; }

    readonly System.Collections.Generic.Dictionary<VideoPlayer, RenderTexture> ownedTextures =
        new System.Collections.Generic.Dictionary<VideoPlayer, RenderTexture>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        MBodyDiagLog.Step("VideoDriver", "Awake");
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        ReleaseAllTextures();
    }

    public void PlayOnPage(GameObject page)
    {
        if (page == null)
            return;

        StopCoroutine(nameof(Idle));
        StartCoroutine(PlayPageRoutine(page));
    }

    IEnumerator Idle()
    {
        yield break;
    }

    public void PlayPlayer(VideoPlayer vp)
    {
        if (vp == null)
            return;

        StartCoroutine(PlayOneRoutine(vp, vp.transform.root.name));
    }

    IEnumerator PlayPageRoutine(GameObject page)
    {
        yield return null;

        var players = page.GetComponentsInChildren<VideoPlayer>(true);
        MBodyDiagLog.Step("VideoDriver", $"PlayPage name={page.name} id={page.GetInstanceID()} players={players.Length}");

        for (var i = 0; i < players.Length; i++)
        {
            if (players[i] == null)
                continue;

            yield return PlayOneRoutine(players[i], page.name);
        }
    }

    IEnumerator PlayOneRoutine(VideoPlayer vp, string pageName)
    {
        if (!HasSource(vp))
        {
            MBodyDiagLog.Warn("VideoDriver", $"No source on '{vp.gameObject.name}' page={pageName}");
            yield break;
        }

        if (vp.isPlaying)
            vp.Stop();

        var clipInfo = vp.clip != null
            ? $"{vp.clip.name} {vp.clip.width}x{vp.clip.height}"
            : vp.url;
        MBodyDiagLog.Step("VideoDriver", $"Prepare '{vp.gameObject.name}' page={pageName} source={clipInfo}");

        ConfigureDisplay(vp);
        vp.playOnAwake = false;
        vp.skipOnDrop = true;
        vp.waitForFirstFrame = true;

        vp.Prepare();
        var timeout = 15f;
        while (!vp.isPrepared && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!vp.isPrepared)
        {
            MBodyDiagLog.Error("VideoDriver", $"Prepare timeout '{vp.gameObject.name}' page={pageName} source={clipInfo}");
            yield break;
        }

        vp.Play();
        var frameWait = 3f;
        while (vp.isPlaying && vp.frame <= 0 && frameWait > 0f)
        {
            frameWait -= Time.unscaledDeltaTime;
            yield return null;
        }

        MBodyDiagLog.Step("VideoDriver", $"Playing '{vp.gameObject.name}' page={pageName} frame={vp.frame} isPlaying={vp.isPlaying} tex={(vp.targetTexture != null)}");
    }

    static bool HasSource(VideoPlayer vp)
    {
        if (vp.source == VideoSource.Url)
            return !string.IsNullOrEmpty(vp.url);

        return vp.clip != null;
    }

    void ConfigureDisplay(VideoPlayer vp)
    {
        var width = 1280;
        var height = 720;
        if (vp.clip != null)
        {
            width = Mathf.Max(320, (int)vp.clip.width);
            height = Mathf.Max(180, (int)vp.clip.height);
        }

        var scale = Mathf.Min(1f, 1920f / width, 1080f / height);
        width = Mathf.Max(320, Mathf.RoundToInt(width * scale));
        height = Mathf.Max(180, Mathf.RoundToInt(height * scale));

        ReleaseTexture(vp);
        var rt = new RenderTexture(width, height, 0, RenderTextureFormat.Default)
        {
            name = $"PageVideo_{vp.gameObject.name}",
            useMipMap = false,
            autoGenerateMips = false
        };
        rt.Create();
        ownedTextures[vp] = rt;

        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetTexture = rt;
        vp.aspectRatio = VideoAspectRatio.FitInside;

        BindDisplayTexture(vp, rt);
        FitRect(vp.GetComponent<RectTransform>(), vp.clip, rt);
        MBodyDiagLog.Step("VideoDriver", $"Display '{vp.gameObject.name}' rt={width}x{height}");
    }

    static void BindDisplayTexture(VideoPlayer vp, RenderTexture rt)
    {
        var raw = vp.GetComponent<RawImage>();
        if (raw != null)
        {
            raw.texture = rt;
            raw.raycastTarget = false;
            raw.color = Color.white;
            raw.enabled = true;
            MBodyDiagLog.Step("VideoDriver", $"Bound RawImage on '{vp.gameObject.name}'");
            return;
        }

        var image = vp.GetComponent<Image>();
        if (image == null)
        {
            MBodyDiagLog.Warn("VideoDriver", $"No Image/RawImage on '{vp.gameObject.name}'");
            return;
        }

        image.raycastTarget = false;
        image.enabled = true;
        image.sprite = null;

        if (image.material != null && image.material.HasProperty("_MainTex"))
        {
            image.material = Instantiate(image.material);
            image.material.mainTexture = rt;
            image.SetMaterialDirty();
            MBodyDiagLog.Step("VideoDriver", $"Bound Image material on '{vp.gameObject.name}' shader={image.material.shader.name}");
            return;
        }

        MBodyDiagLog.Warn("VideoDriver", $"Image on '{vp.gameObject.name}' has no _MainTex material; adding RawImage");
        raw = vp.gameObject.AddComponent<RawImage>();
        raw.texture = rt;
        raw.raycastTarget = false;
        raw.color = Color.white;
    }

    static void FitRect(RectTransform rect, VideoClip clip, RenderTexture rt)
    {
        if (rect == null)
            return;

        var parent = rect.parent as RectTransform;
        if (parent == null)
            return;

        var availW = parent.rect.width;
        var availH = parent.rect.height;
        if (availW <= 0f || availH <= 0f)
        {
            availW = Screen.width > 0 ? Screen.width : 1920f;
            availH = Screen.height > 0 ? Screen.height : 1080f;
            MBodyDiagLog.Warn("VideoDriver", $"FitRect parent size zero on '{rect.name}'; using {availW}x{availH}");
        }

        float videoAspect = 16f / 9f;
        if (rt != null && rt.height > 0)
            videoAspect = rt.width / (float)rt.height;
        else if (clip != null && clip.height > 0)
            videoAspect = clip.width / (float)clip.height;

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

        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(w, h);
    }

    void ReleaseTexture(VideoPlayer vp)
    {
        if (vp == null || !ownedTextures.TryGetValue(vp, out var rt))
            return;

        if (vp.targetTexture == rt)
            vp.targetTexture = null;

        if (rt != null)
        {
            rt.Release();
            Destroy(rt);
        }

        ownedTextures.Remove(vp);
    }

    void ReleaseAllTextures()
    {
        foreach (var pair in ownedTextures)
        {
            if (pair.Value == null)
                continue;

            if (pair.Key != null && pair.Key.targetTexture == pair.Value)
                pair.Key.targetTexture = null;

            pair.Value.Release();
            Destroy(pair.Value);
        }

        ownedTextures.Clear();
    }
}
