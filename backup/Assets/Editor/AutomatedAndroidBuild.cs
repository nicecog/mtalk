using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AutomatedAndroidBuild
{
    private const string BootstrapScenePath = "Assets/Scenes/AndroidBootstrap.unity";
    private const string MainSceneName = "MBody";

    public static void BuildLatestStableAndroidApk()
    {
        BuildAndroidApk(development: false);
    }

    public static void BuildLatestStableAndRunOnDevice()
    {
        BuildAndroidApk(development: false, BuildOptions.AutoRunPlayer);
        PushObbToConnectedDevice();
    }

    public static void BuildProfilingAndroidApk()
    {
        BuildAndroidApk(development: true);
    }

    [MenuItem("MBody/Build/Android APK (Release)")]
    static void MenuBuildRelease() => BuildLatestStableAndroidApk();

    [MenuItem("MBody/Build/Android Build And Run + OBB")]
    static void MenuBuildAndRun() => BuildLatestStableAndRunOnDevice();

    [MenuItem("MBody/Build/Android Build And Run + OBB", validate = true)]
    static bool ValidateMenuBuildAndRun() => CanStartAndroidBuild();

    [MenuItem("MBody/Build/Android APK (Release)", validate = true)]
    static bool ValidateMenuBuildRelease() => CanStartAndroidBuild();

    static bool CanStartAndroidBuild() =>
        !EditorApplication.isCompiling && !EditorApplication.isUpdating;

    static void WaitForEditorReady()
    {
        AssetDatabase.Refresh();
        while (EditorApplication.isCompiling || EditorApplication.isUpdating)
            System.Threading.Thread.Sleep(100);
    }

    static void BuildAndroidApk(bool development, BuildOptions extraOptions = BuildOptions.None)
    {
        WaitForEditorReady();
        EnsureBootstrapScene();

        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .Where(path => path != BootstrapScenePath)
            .ToArray();

        if (scenes.Length == 0)
            throw new System.Exception("No enabled scenes in EditorBuildSettings.");

        scenes = new[] { BootstrapScenePath }.Concat(scenes).ToArray();

        var outputDirectory = Path.Combine("Build", "Android");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(
            outputDirectory,
            development ? "MBody-profiling-dev.apk" : "MBody-latest-stable.apk");

        EditorUserBuildSettings.buildAppBundle = false;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.CAU.MBody");
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.bundleVersion = "1";
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.Android.splitApplicationBinary = true;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;

        var arch = PlayerSettings.Android.targetArchitectures;
        if ((arch & AndroidArchitecture.ARMv7) == 0)
            throw new System.Exception("ARMv7 (armeabi-v7a) must be enabled for Galaxy Tab A8.");

        UnityEngine.Debug.Log(
            $"[AutomatedAndroidBuild] arch={arch} (ARMv7={(arch & AndroidArchitecture.ARMv7) != 0}, " +
            $"ARM64={(arch & AndroidArchitecture.ARM64) != 0}), IL2CPP, minSdk=25, targetSdk=36");

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = development
                ? BuildOptions.Development | BuildOptions.ConnectWithProfiler | BuildOptions.AllowDebugging | extraOptions
                : extraOptions
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        if (report.summary.result != BuildResult.Succeeded)
            throw new System.Exception($"Android build failed: {report.summary.result}");

        PublishDeviceObb(outputDirectory, outputPath);
        UnityEngine.Debug.Log($"Android APK build succeeded: {outputPath}");
    }

    static void PublishDeviceObb(string outputDirectory, string apkPath)
    {
        var apkFileName = Path.GetFileNameWithoutExtension(apkPath);
        var sourceObb = Path.Combine(outputDirectory, apkFileName + ".main.obb");
        if (!File.Exists(sourceObb))
            throw new System.Exception("OBB not found after build: " + sourceObb);

        var versionCode = PlayerSettings.Android.bundleVersionCode;
        var packageId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        var deviceObbName = $"main.{versionCode}.{packageId}.obb";
        var deviceObbPath = Path.Combine(outputDirectory, deviceObbName);
        File.Copy(sourceObb, deviceObbPath, true);
        UnityEngine.Debug.Log($"Android OBB device name: {deviceObbPath}");
    }

    private static void EnsureBootstrapScene()
    {
        if (File.Exists(BootstrapScenePath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(BootstrapScenePath) ?? "Assets/Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var bootstrapObject = new GameObject("AndroidBootstrapLoader");
        bootstrapObject.AddComponent<AndroidBootstrapLoader>();

        if (!EditorSceneManager.SaveScene(scene, BootstrapScenePath))
            throw new System.Exception($"Failed to create bootstrap scene at {BootstrapScenePath}.");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created Android bootstrap scene for split binary build. Main scene: {MainSceneName}");
    }

    static void PushObbToConnectedDevice()
    {
        var outputDirectory = Path.Combine("Build", "Android");
        var versionCode = PlayerSettings.Android.bundleVersionCode;
        var packageId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        var obbPath = Path.Combine(outputDirectory, $"main.{versionCode}.{packageId}.obb");
        if (!File.Exists(obbPath))
            throw new System.Exception("Device OBB missing: " + obbPath);

        var sdk = EditorPrefs.GetString("AndroidSdkRoot");
        if (string.IsNullOrEmpty(sdk))
            sdk = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk");

        var adb = Path.Combine(sdk, "platform-tools", "adb.exe");
        if (!File.Exists(adb))
            throw new System.Exception("adb not found: " + adb);

        var remoteDir = $"/sdcard/Android/obb/{packageId}";
        var remoteObb = $"{remoteDir}/main.{versionCode}.{packageId}.obb";
        RunProcess(adb, $"shell mkdir -p \"{remoteDir}\"");
        RunProcess(adb, $"push \"{Path.GetFullPath(obbPath)}\" \"{remoteObb}\"");
        UnityEngine.Debug.Log($"OBB pushed to device: {remoteObb}");
    }

    static void RunProcess(string exe, string args)
    {
        var psi = new System.Diagnostics.ProcessStartInfo(exe, args)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using (var p = System.Diagnostics.Process.Start(psi))
        {
            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (!string.IsNullOrEmpty(stdout))
                UnityEngine.Debug.Log(stdout.Trim());
            if (!string.IsNullOrEmpty(stderr))
                UnityEngine.Debug.LogWarning(stderr.Trim());
            if (p.ExitCode != 0)
                throw new System.Exception($"{exe} {args} failed ({p.ExitCode})");
        }
    }
}
