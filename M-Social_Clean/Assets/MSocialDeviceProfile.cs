using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Galaxy Tab A 8.0 (2GB RAM) 등 저사양 기기 — 메모리·비디오 버퍼 완화.
/// </summary>
public static class MSocialDeviceProfile
{
    public static int VideoWidth { get; private set; } = 1920;
    public static int VideoHeight { get; private set; } = 1080;
    public static bool IsLowMemoryDevice { get; private set; }

    static readonly HashSet<int> RuntimeRenderTextureIds = new HashSet<int>();

    public static bool IsRuntimeRenderTexture(RenderTexture rt) =>
        rt != null && RuntimeRenderTextureIds.Contains(rt.GetInstanceID());

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        int ramMb = SystemInfo.systemMemorySize;
        Application.targetFrameRate = 30;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // Tab A 8.0 2019: 2GB — 3.5GB 미만이면 저사양 프로필
        if (ramMb > 0 && ramMb < 3500)
        {
            IsLowMemoryDevice = true;
            if (ramMb < 2800)
            {
                VideoWidth = 960;
                VideoHeight = 540;
            }
            else
            {
                VideoWidth = 1280;
                VideoHeight = 720;
            }
            // UI 텍스처 풀 해상도 — Stage1 맵·라벨 잘림 방지 (영상만 저해상도 RT)
            QualitySettings.masterTextureLimit = 0;

            QualitySettings.pixelLightCount = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            Application.lowMemory += OnLowMemory;
            MSocialStartupLog.Log("Low-memory profile RAM=" + ramMb + "MB video=" + VideoWidth + "x" + VideoHeight + " texLimit=" + QualitySettings.masterTextureLimit);
        }
        else
        {
            // 고화소 태블릿 — PrepareUrl 경로 1080p 유지
            VideoWidth = 1920;
            VideoHeight = 1080;
            MSocialStartupLog.Log("Standard Android profile RAM=" + ramMb + "MB video=" + VideoWidth + "x" + VideoHeight);
        }
#endif
    }

    static void OnLowMemory()
    {
        MSocialStartupLog.Log("Application.lowMemory — unloading assets");
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

    public static RenderTexture CreateVideoRenderTexture()
    {
        var rt = new RenderTexture(VideoWidth, VideoHeight, 0, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 1;
        rt.Create();
        RuntimeRenderTextureIds.Add(rt.GetInstanceID());
        return rt;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AfterSceneLoadCleanup()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsLowMemoryDevice)
            return;
        // Additive 씬 로드 직후 GC 금지 — Stage1 UI/스프라이트가 사라지는 현상 방지
        if (SceneManager.sceneCount > 1)
            return;
#endif
    }

    public static void ApplyBootstrapCanvas(Transform canvasRoot)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsLowMemoryDevice || canvasRoot == null)
            return;

        var scaler = canvasRoot.GetComponent<CanvasScaler>();
        if (scaler == null)
            return;
        // Stage1/2 페이지도 같은 Canvas 사용 — 디자인 해상도 유지 (축소 시 '학교' 등 잘림)
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2800f, 1752f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
        MSocialStartupLog.Log("CanvasScaler design 2800x1752 match=width");
#endif
    }
}
