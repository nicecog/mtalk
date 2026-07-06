using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 저사양 기기만 씬에 박힌 1080p RenderTexture 참조를 해제하고 런타임 RT로 전환.
/// RAM ≥ 3.5GB(고화소)는 씬 RT·화질 그대로 유지.
/// </summary>
public static class MSocialVideoRtOptimizer
{
    const int LargeRtMinEdge = 1280;

    public static void OptimizeSceneIfNeeded(Scene scene)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!MSocialDeviceProfile.IsLowMemoryDevice || !scene.IsValid())
            return;

        int released = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var vp in root.GetComponentsInChildren<VideoPlayer>(true))
            {
                if (ReleaseLargeSceneRenderTexture(vp))
                    released++;
            }
        }
        if (released > 0)
            MSocialStartupLog.Log("VideoRtOptimizer released " + released + " scene RT(s) in " + scene.name);
#endif
    }

    static bool ReleaseLargeSceneRenderTexture(VideoPlayer vp)
    {
        if (vp == null)
            return false;
        if (vp.GetComponentInParent<ScenePlayer>() != null)
            return false;

        var rt = vp.targetTexture;
        if (rt == null)
            return false;
        if (MSocialDeviceProfile.IsRuntimeRenderTexture(rt))
            return false;
        if (rt.width < LargeRtMinEdge && rt.height < LargeRtMinEdge)
            return false;

        var raw = MSocialVideoPlayerHelper.FindLinkedRawImage(vp);
        if (raw != null && raw.texture == rt)
            raw.texture = null;

        vp.Stop();
        vp.playOnAwake = false;
        vp.targetTexture = null;
        return true;
    }
}
