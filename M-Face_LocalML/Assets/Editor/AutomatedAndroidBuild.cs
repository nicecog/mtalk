using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AutomatedAndroidBuild
{
    public static void BuildLatestStableAndroidApk()
    {
        MFaceAppIconSetup.ApplyAndroidIcons();

        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new System.Exception("No enabled scenes in EditorBuildSettings.");

        var outputDirectory = Path.Combine("Build", "Android");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "MFace-latest-stable.apk");

        EditorUserBuildSettings.buildAppBundle = false;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.Spike.MFace");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        // Galaxy Tab A 8.0 (2019) 등 32-bit Android 태블릿 호환 (armeabi-v7a + arm64-v8a)
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
#if UNITY_2022_2_OR_NEWER
        PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
#endif

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        if (report.summary.result != BuildResult.Succeeded)
            throw new System.Exception($"Android build failed: {report.summary.result}");

        Debug.Log($"Android APK build succeeded: {outputPath}");
    }
}
