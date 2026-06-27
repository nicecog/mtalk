using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Mediapipe.BlazePose;

public class PoseVisuallizer : MonoBehaviour
{
    const int LandmarkCount = 34;
    const int FloatsPerLandmark = 4;
    const int ScratchLength = LandmarkCount * FloatsPerLandmark;
    const int LeftWristIndex = 19;
    const int RightWristIndex = 20;
    const int HumanExistIndex = 33;

    [SerializeField] WebCamInput webCamInput;
    [SerializeField] RawImage inputImageUI;
    [SerializeField] BlazePoseResource blazePoseResource;
    [SerializeField] BlazePoseModel poseLandmarkModel;

    BlazePoseDetecter detecter;
    FlowManager fm;
    Coroutine poseLoopRoutine;
    int loopGeneration;
    bool readbackPending;

    readonly float[] landmarkScratch = new float[ScratchLength];
    bool hasValidPose;
    bool processingActive;

    Vector2 displayLeft;
    Vector2 displayRight;
    Vector2 targetLeft;
    Vector2 targetRight;
    float confidence;

    void OnEnable()
    {
        EnsureDetector();
    }

    void OnDisable()
    {
        StopPoseLoop();
        processingActive = false;
        hasValidPose = false;
    }

    void OnDestroy()
    {
        StopPoseLoop();
        ReleaseDetector();
    }

    void EnsureDetector()
    {
        if (detecter != null)
            return;

        var resource = blazePoseResource != null ? blazePoseResource : BlazePoseResourceProvider.Load();
        if (resource == null)
            return;

        detecter = new BlazePoseDetecter(resource, poseLandmarkModel);
        if (fm == null)
            fm = FindFirstObjectByType<FlowManager>();
    }

    void ReleaseDetector()
    {
        if (detecter == null)
            return;

        detecter.Dispose();
        detecter = null;
        hasValidPose = false;
    }

    public void SetProcessing(bool active)
    {
        if (processingActive == active)
            return;

        processingActive = active;
        StopPoseLoop();

        if (!active) {
            hasValidPose = false;
            return;
        }

        EnsureDetector();
        if (detecter == null)
            return;

        loopGeneration++;
        var gen = loopGeneration;
        poseLoopRoutine = StartCoroutine(DelayedPoseLoopStart(gen));
    }

    IEnumerator DelayedPoseLoopStart(int generation)
    {
        var settleFrames = ResolveSettleFrames();
        for (int i = 0; i < settleFrames; i++)
            yield return null;

        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        if (!processingActive || generation != loopGeneration || detecter == null)
            yield break;

        poseLoopRoutine = StartCoroutine(PoseLoop());
    }

    int ResolveSettleFrames()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var bm = FindFirstObjectByType<BodyManager>(FindObjectsInactive.Include);
        if (bm != null && bm.playTimes > 0)
            return 10;
        return 4;
#else
        return 1;
#endif
    }

    void StopPoseLoop()
    {
        loopGeneration++;
        readbackPending = false;
        if (poseLoopRoutine != null) {
            StopCoroutine(poseLoopRoutine);
            poseLoopRoutine = null;
        }
    }

    IEnumerator PoseLoop()
    {
        var endOfFrame = new WaitForEndOfFrame();
        var interval = ResolveProcessInterval();
        var useAsyncReadback = SystemInfo.supportsAsyncGPUReadback;

        while (processingActive && detecter != null) {
            yield return endOfFrame;

            if (!processingActive || detecter == null)
                yield break;

            var tex = webCamInput != null ? webCamInput.PoseInputTexture : null;
            if (tex == null || tex.width <= 16 || tex.height <= 16) {
                yield return null;
                continue;
            }

            detecter.ProcessImage(tex, poseLandmarkModel);
            yield return endOfFrame;

            if (!processingActive || detecter == null)
                yield break;

            if (useAsyncReadback) {
                readbackPending = true;
                AsyncGPUReadback.Request(detecter.outputBuffer, OnLandmarkReadback);
                while (readbackPending) {
                    if (!processingActive)
                        yield break;
                    yield return null;
                }
            } else {
                ReadLandmarksFromGpu();
            }

            for (int i = 0; i < interval; i++) {
                if (!processingActive)
                    yield break;
                yield return null;
            }
        }

        poseLoopRoutine = null;
    }

    void OnLandmarkReadback(AsyncGPUReadbackRequest request)
    {
        readbackPending = false;
        if (!processingActive || request.hasError || detecter == null)
            return;

        var native = request.GetData<float>();
        var count = Mathf.Min(native.Length, landmarkScratch.Length);
        for (int i = 0; i < count; i++)
            landmarkScratch[i] = native[i];

        ApplyLandmarks(landmarkScratch);
    }

    int ResolveProcessInterval()
    {
        var interval = PerformanceManager.Instance != null
            ? PerformanceManager.Instance.EffectivePoseProcessInterval
            : 1;
#if UNITY_ANDROID && !UNITY_EDITOR
        interval = Mathf.Max(interval, 8);
#endif
        return Mathf.Max(1, interval);
    }

    void LateUpdate()
    {
        if (webCamInput == null || inputImageUI == null)
            return;

        if (!webCamInput.HasValidFrame)
            return;

        var tex = webCamInput.inputImageTexture;
        if (tex == null || tex.width <= 16 || tex.height <= 16)
            return;

        inputImageUI.texture = tex;
        inputImageUI.enabled = true;

        if (!processingActive || !hasValidPose || fm == null)
            return;

        displayLeft = Vector2.Lerp(displayLeft, targetLeft, 0.35f);
        displayRight = Vector2.Lerp(displayRight, targetRight, 0.35f);
        fm.UpdateAI(displayLeft, displayRight, confidence);
    }

    void ReadLandmarksFromGpu()
    {
        if (detecter == null)
            return;

        detecter.outputBuffer.GetData(landmarkScratch);
        ApplyLandmarks(landmarkScratch);
    }

    void ApplyLandmarks(float[] data)
    {
        targetLeft = LandmarkToUi(
            data[LeftWristIndex * FloatsPerLandmark],
            data[LeftWristIndex * FloatsPerLandmark + 1]);
        targetRight = LandmarkToUi(
            data[RightWristIndex * FloatsPerLandmark],
            data[RightWristIndex * FloatsPerLandmark + 1]);
        confidence = data[HumanExistIndex * FloatsPerLandmark];

        if (!hasValidPose) {
            displayLeft = targetLeft;
            displayRight = targetRight;
        }

        hasValidPose = true;
    }

    static Vector2 LandmarkToUi(float normalizedX, float normalizedY)
    {
        return new Vector2(-2800f * (normalizedX - 0.5f), 1752f * (normalizedY - 0.5f));
    }
}
