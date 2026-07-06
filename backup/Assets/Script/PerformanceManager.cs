using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Startup micro-benchmark (tier selection) and runtime load adaptation.
/// </summary>
public sealed class PerformanceManager : MonoBehaviour
{
    public enum PerformanceTier
    {
        Low = 0,
        Medium = 1,
        High = 2,
        UltraLow = 3
    }

    const string PrefTierKey = "MBody_PerfTier";
    const string PrefVersionKey = "MBody_PerfVersion";
    const int BenchmarkVersion = 2;

    const int BenchmarkBlitIterations = 24;
    const int BenchmarkReadbackIterations = 12;

    public static PerformanceManager Instance { get; private set; }

    public bool IsReady { get; private set; }
    public PerformanceTier BaseTier { get; private set; }
    public int RuntimeStressLevel { get; private set; }

    public Vector2Int CameraResolution { get; private set; }
    public int TargetFrameRate { get; private set; }
    public int BasePoseProcessInterval { get; private set; }
    public int RecordWidth { get; private set; }
    public int RecordHeight { get; private set; }
    public float RecordFps { get; private set; }

    public bool IsRecordingActive { get; private set; }

    public int EffectivePoseProcessInterval =>
        BasePoseProcessInterval + RuntimeStressLevel + (IsRecordingActive ? 1 : 0);

    float frameTimeAccum;
    int frameTimeSamples;
    float adjustmentTimer;
    float statsLogTimer;
    float lastBenchmarkBlitMs;
    float lastBenchmarkReadbackMs;
    float lastLoggedAvgFrameMs;

    public static PerformanceManager EnsureExists()
    {
        if (Instance != null)
            return Instance;

        var go = new GameObject(nameof(PerformanceManager));
        return go.AddComponent<PerformanceManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!IsReady)
            return;

        RegisterFrame(Time.unscaledDeltaTime);

        adjustmentTimer += Time.unscaledDeltaTime;
        if (adjustmentTimer >= 2f) {
            adjustmentTimer = 0f;
            EvaluateRuntimeLoad();
        }

        statsLogTimer += Time.unscaledDeltaTime;
        if (statsLogTimer >= 5f) {
            statsLogTimer = 0f;
            LogRuntimeStats();
        }
    }

    public string GetProfileSummary()
    {
        return
            $"tier={BaseTier}, stress={RuntimeStressLevel}, poseInterval={EffectivePoseProcessInterval}, " +
            $"camera={CameraResolution.x}x{CameraResolution.y}, record={RecordWidth}x{RecordHeight}@{RecordFps:0.#}fps, " +
            $"recording={IsRecordingActive}, avgFrameMs={lastLoggedAvgFrameMs:F1}, device={SystemInfo.deviceModel}";
    }

    public IEnumerator InitializeAsync()
    {
        if (IsReady)
            yield break;

        if (TryLoadCachedTier())
        {
            ApplyTier(BaseTier);
            IsReady = true;
            Debug.Log($"[PerformanceManager] Loaded cached tier: {BaseTier} | {GetProfileSummary()}");
            yield break;
        }

        yield return RunBenchmarkCoroutine();
        SaveTier(BaseTier);
        ApplyTier(BaseTier);
        IsReady = true;
        Debug.Log(
            $"[PerformanceManager] Benchmark complete. Tier={BaseTier}, blit={lastBenchmarkBlitMs:F2}ms, readback={lastBenchmarkReadbackMs:F2}ms, ram={SystemInfo.systemMemorySize}MB | {GetProfileSummary()}");
    }

    public void RegisterFrame(float unscaledDeltaTime)
    {
        if (!IsReady || unscaledDeltaTime <= 0f)
            return;

        frameTimeAccum += unscaledDeltaTime;
        frameTimeSamples++;
    }

    public void SetRecordingActive(bool active)
    {
        IsRecordingActive = active;
    }

    bool TryLoadCachedTier()
    {
        if (!PlayerPrefs.HasKey(PrefTierKey) || !PlayerPrefs.HasKey(PrefVersionKey))
            return false;

        if (PlayerPrefs.GetInt(PrefVersionKey) != BenchmarkVersion)
            return false;

        var tierValue = PlayerPrefs.GetInt(PrefTierKey, (int)PerformanceTier.Medium);
        tierValue = Mathf.Clamp(tierValue, (int)PerformanceTier.Low, (int)PerformanceTier.UltraLow);
        BaseTier = (PerformanceTier)tierValue;
        RuntimeStressLevel = 0;
        return true;
    }

    void SaveTier(PerformanceTier tier)
    {
        PlayerPrefs.SetInt(PrefTierKey, (int)tier);
        PlayerPrefs.SetInt(PrefVersionKey, BenchmarkVersion);
        PlayerPrefs.Save();
    }

    IEnumerator RunBenchmarkCoroutine()
    {
        RuntimeStressLevel = 0;
        lastBenchmarkBlitMs = MeasureBlitMilliseconds();
        yield return null;

        if (SystemInfo.supportsAsyncGPUReadback)
            yield return MeasureReadbackMillisecondsCoroutine(ms => lastBenchmarkReadbackMs = ms);
        else
            lastBenchmarkReadbackMs = 999f;

        BaseTier = ClassifyTier(lastBenchmarkBlitMs, lastBenchmarkReadbackMs);
    }

    static PerformanceTier ClassifyTier(float blitMs, float readbackMs)
    {
        var ramMb = SystemInfo.systemMemorySize;
        var hasAsync = SystemInfo.supportsAsyncGPUReadback;
        var hasCompute = SystemInfo.supportsComputeShaders;

        if (ramMb > 0 && ramMb < 2500)
            return PerformanceTier.UltraLow;

        if (!hasAsync || !hasCompute || ramMb > 0 && ramMb < 3000)
            return PerformanceTier.Low;

        if (blitMs > 2.8f || readbackMs > 9f || ramMb > 0 && ramMb < 4500)
            return PerformanceTier.Low;

        if (blitMs < 1.2f && readbackMs < 4.5f && ramMb >= 5000)
            return PerformanceTier.High;

        return PerformanceTier.Medium;
    }

    float MeasureBlitMilliseconds()
    {
        const int width = 1280;
        const int height = 720;

        var source = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color32[width * height];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(40, 80, 120, 255);
        source.SetPixels32(pixels);
        source.Apply(false, true);

        var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;

        for (var i = 0; i < 5; i++)
            Graphics.Blit(source, target);

        var start = Time.realtimeSinceStartup;
        for (var i = 0; i < BenchmarkBlitIterations; i++)
            Graphics.Blit(source, target);
        var elapsedMs = (Time.realtimeSinceStartup - start) * 1000f / BenchmarkBlitIterations;

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(target);
        Destroy(source);

        return elapsedMs;
    }

    IEnumerator MeasureReadbackMillisecondsCoroutine(System.Action<float> onComplete)
    {
        const int width = 512;
        const int height = 512;

        var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(Texture2D.whiteTexture, rt);

        for (var i = 0; i < 3; i++)
        {
            var warmup = AsyncGPUReadback.Request(rt);
            while (!warmup.done)
                yield return null;
        }

        var accum = 0f;
        for (var i = 0; i < BenchmarkReadbackIterations; i++)
        {
            var start = Time.realtimeSinceStartup;
            var request = AsyncGPUReadback.Request(rt);
            while (!request.done)
                yield return null;

            if (request.hasError)
            {
                accum = 999f;
                break;
            }

            accum += (Time.realtimeSinceStartup - start) * 1000f;
        }

        RenderTexture.ReleaseTemporary(rt);
        onComplete?.Invoke(accum / BenchmarkReadbackIterations);
    }

    void EvaluateRuntimeLoad()
    {
        if (frameTimeSamples <= 0)
            return;

        var avgFrameTime = frameTimeAccum / frameTimeSamples;
        frameTimeAccum = 0f;
        frameTimeSamples = 0;

        lastLoggedAvgFrameMs = avgFrameTime * 1000f;

        if (avgFrameTime > 0.038f && RuntimeStressLevel < 2)
        {
            RuntimeStressLevel++;
            Debug.Log($"[PerformanceManager] Runtime downgrade stress={RuntimeStressLevel}, avgFrame={lastLoggedAvgFrameMs:F1}ms | {GetProfileSummary()}");
        }
        else if (avgFrameTime < 0.03f && RuntimeStressLevel > 0)
        {
            RuntimeStressLevel--;
            Debug.Log($"[PerformanceManager] Runtime upgrade stress={RuntimeStressLevel}, avgFrame={lastLoggedAvgFrameMs:F1}ms | {GetProfileSummary()}");
        }
    }

    void LogRuntimeStats()
    {
        if (frameTimeSamples <= 0)
            return;

        lastLoggedAvgFrameMs = frameTimeAccum / frameTimeSamples * 1000f;
        Debug.Log($"[PerfStats] {GetProfileSummary()}");
    }

    void ApplyTier(PerformanceTier tier)
    {
        switch (tier)
        {
            case PerformanceTier.UltraLow:
                CameraResolution = new Vector2Int(640, 480);
                TargetFrameRate = 30;
                BasePoseProcessInterval = 4;
                RecordWidth = 640;
                RecordHeight = 480;
                RecordFps = 15f;
                QualitySettings.SetQualityLevel(0, true);
                break;
            case PerformanceTier.Low:
                CameraResolution = new Vector2Int(1280, 720);
                TargetFrameRate = 30;
                BasePoseProcessInterval = 2;
                RecordWidth = 1280;
                RecordHeight = 720;
                RecordFps = 20f;
                QualitySettings.SetQualityLevel(1, true);
                break;
            case PerformanceTier.High:
                CameraResolution = new Vector2Int(1920, 1080);
                TargetFrameRate = 30;
                BasePoseProcessInterval = 1;
                RecordWidth = 1920;
                RecordHeight = 1080;
                RecordFps = 25f;
                QualitySettings.SetQualityLevel(4, true);
                break;
            default:
                CameraResolution = new Vector2Int(1280, 720);
                TargetFrameRate = 30;
                BasePoseProcessInterval = 1;
                RecordWidth = 1280;
                RecordHeight = 720;
                RecordFps = 25f;
                QualitySettings.SetQualityLevel(2, true);
                break;
        }

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = TargetFrameRate;
    }
}
