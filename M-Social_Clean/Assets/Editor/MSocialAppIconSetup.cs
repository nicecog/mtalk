using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

public static class MSocialAppIconSetup
{
    private const string IconPath = "Assets/AppIcon/MSocialAppIcon.png";

    [MenuItem("MSocial/Apply App Icon")]
    public static void ApplyAndroidIcons()
    {
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (icon == null)
        {
            Debug.LogError($"[MSocialAppIconSetup] Icon not found: {IconPath}");
            return;
        }

        ApplySingleLayerIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Legacy, icon);
        ApplySingleLayerIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Round, icon);
        ApplyAdaptiveIcons(NamedBuildTarget.Android, icon);

        AssetDatabase.SaveAssets();
        Debug.Log("[MSocialAppIconSetup] Android app icon applied.");
    }

    private static void ApplySingleLayerIcons(NamedBuildTarget target, PlatformIconKind kind, Texture2D texture)
    {
        var icons = PlayerSettings.GetPlatformIcons(target, kind);
        if (icons == null || icons.Length == 0)
            return;

        for (var i = 0; i < icons.Length; i++)
            icons[i].SetTextures(new[] { texture });

        PlayerSettings.SetPlatformIcons(target, kind, icons);
    }

    private static void ApplyAdaptiveIcons(NamedBuildTarget target, Texture2D texture)
    {
        var kind = AndroidPlatformIconKind.Adaptive;
        var icons = PlayerSettings.GetPlatformIcons(target, kind);
        if (icons == null || icons.Length == 0)
            return;

        for (var i = 0; i < icons.Length; i++)
            icons[i].SetTextures(new[] { texture, texture });

        PlayerSettings.SetPlatformIcons(target, kind, icons);
    }
}
