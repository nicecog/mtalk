using Mediapipe.BlazePose;
using UnityEngine;

public static class BlazePoseResourceProvider
{
    const string ResourcePath = "Pose/BlazePose";

    static BlazePoseResource cached;

    public static BlazePoseResource Load()
    {
        if (cached != null)
            return cached;

        cached = Resources.Load<BlazePoseResource>(ResourcePath);
        if (cached == null)
            Debug.LogError("[BlazePose] Resources.Load failed at '" + ResourcePath + "'. Ensure Assets/Resources/Pose/BlazePose.asset exists.");
        return cached;
    }

    public static void Unload()
    {
        if (cached == null)
            return;

        Resources.UnloadAsset(cached);
        cached = null;
        Resources.UnloadUnusedAssets();
    }
}
