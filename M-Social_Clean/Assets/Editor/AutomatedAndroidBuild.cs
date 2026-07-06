using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public static class AutomatedAndroidBuild
{
    public static void BuildLatestStableAndroidApk()
    {
        MSocialAppIconSetup.ApplyAndroidIcons();

        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new System.Exception("No enabled scenes in EditorBuildSettings.");

        var outputDirectory = Path.Combine("Build", "Android");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "MSocial-latest-stable.apk");

        EditorUserBuildSettings.buildAppBundle = false;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.Spike.MSocial");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33;
        PlayerSettings.Android.bundleVersionCode = 26;
        PlayerSettings.bundleVersion = "1.38";
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Minimal);
        // Mali-G71 등 구형 GPU: Vulkan 대신 OpenGLES3 고정
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
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
