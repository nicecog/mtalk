using System.Collections;
using UnityEngine;
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
        if (active)
            StartPoseLoop();
        else
            StopPoseLoop();

        if (!active)
            hasValidPose = false;
    }

    void StartPoseLoop()
    {
        if (poseLoopRoutine != null)
            return;

        EnsureDetector();
        if (detecter == null)
            return;

        poseLoopRoutine = StartCoroutine(PoseLoop());
    }

    void StopPoseLoop()
    {
        if (poseLoopRoutine != null) {
            StopCoroutine(poseLoopRoutine);
            poseLoopRoutine = null;
        }
    }

    IEnumerator PoseLoop()
    {
        var endOfFrame = new WaitForEndOfFrame();
        var interval = ResolveProcessInterval();

        while (processingActive && detecter != null) {
            var tex = webCamInput != null ? webCamInput.inputImageTexture : null;
            if (tex == null || tex.width <= 16 || tex.height <= 16) {
                yield return null;
                continue;
            }

            detecter.ProcessImage(tex, poseLandmarkModel);
            yield return endOfFrame;

            if (!processingActive || detecter == null)
                yield break;

            ReadLandmarksFromGpu();

            for (int i = 0; i < interval; i++) {
                if (!processingActive)
                    yield break;
                yield return null;
            }
        }

        poseLoopRoutine = null;
    }

    int ResolveProcessInterval()
    {
        var interval = PerformanceManager.Instance != null
            ? PerformanceManager.Instance.EffectivePoseProcessInterval
            : 1;
#if UNITY_ANDROID && !UNITY_EDITOR
        interval = Mathf.Max(interval, 6);
#endif
        return Mathf.Max(1, interval);
    }

    void LateUpdate()
    {
        if (webCamInput == null || inputImageUI == null)
            return;

        var tex = webCamInput.inputImageTexture;
        if (tex == null || tex.width <= 16 || tex.height <= 16)
            return;

        inputImageUI.texture = tex;

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

    void OnDestroy()
    {
        StopPoseLoop();
        ReleaseDetector();
    }
}
