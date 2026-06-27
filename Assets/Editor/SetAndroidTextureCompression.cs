using System.IO;
using UnityEditor;
using UnityEngine;

public static class SetAndroidTextureCompression
{
    const int DefaultMaxSize = 1024;

    [MenuItem("MBody/Setup/Apply Android ASTC Texture Compression")]
    public static void Apply()
    {
        var roots = new[]
        {
            "Assets/Images",
            "Assets/DanceVideo",
            "Assets/Images/SoundIcons"
        };

        var changed = 0;
        foreach (var root in roots)
        {
            if (!AssetDatabase.IsValidFolder(root))
                continue;

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { root });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                var settings = importer.GetPlatformTextureSettings("Android");
                var needsUpdate = !settings.overridden
                    || settings.maxTextureSize > DefaultMaxSize
                    || settings.format != TextureImporterFormat.ASTC_6x6;

                if (!needsUpdate)
                    continue;

                settings.overridden = true;
                settings.maxTextureSize = Mathf.Min(settings.maxTextureSize > 0 ? settings.maxTextureSize : DefaultMaxSize, DefaultMaxSize);
                settings.format = TextureImporterFormat.ASTC_6x6;
                importer.SetPlatformTextureSettings(settings);
                importer.SaveAndReimport();
                changed++;
            }
        }

        Debug.Log($"[SetAndroidTextureCompression] Updated {changed} PNG textures for Android ASTC 6x6 max {DefaultMaxSize}.");
    }
}
