#if UNITY_EDITOR
using System.IO;
using Mediapipe.BlazePose;
using UnityEditor;
using UnityEngine;

public static class BlazePoseResourceSetup
{
    const string PackageAssetPath = "Packages/BlazePoseBarracuda/ResourceSet/BlazePose.asset";
    const string ResourcesAssetPath = "Assets/Resources/Pose/BlazePose.asset";

    [MenuItem("MBody/Setup/Copy BlazePose to Resources (lazy load)")]
    public static void CopyBlazePoseToResources()
    {
        var source = AssetDatabase.LoadAssetAtPath<BlazePoseResource>(PackageAssetPath);
        if (source == null) {
            Debug.LogError("Package BlazePose asset not found at " + PackageAssetPath);
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(ResourcesAssetPath));
        if (AssetDatabase.CopyAsset(PackageAssetPath, ResourcesAssetPath))
            Debug.Log("Copied BlazePose to " + ResourcesAssetPath);
        else
            Debug.Log("BlazePose already at " + ResourcesAssetPath + " (copy skipped or updated).");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif
