#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MFaceLocalMLTests
{
    const string MenuRoot = "M-Face/LocalML/";

    [MenuItem(MenuRoot + "Analyze Sample Capture (persistentDataPath)")]
    static void AnalyzeLatestCapture()
    {
        string dir = Application.persistentDataPath;
        var files = Directory.GetFiles(dir, "*_Stage*.jpg");
        if (files.Length == 0)
        {
            EditorUtility.DisplayDialog("M-Face LocalML", "No Stage capture JPG in:\n" + dir, "OK");
            return;
        }

        string latest = files[0];
        foreach (var f in files)
            if (File.GetLastWriteTime(f) > File.GetLastWriteTime(latest))
                latest = f;

        RunAnalysis(latest, correctType: 0);
    }

    [MenuItem(MenuRoot + "Analyze Selected Texture")]
    static void AnalyzeSelectedTexture()
    {
        var tex = Selection.activeObject as Texture2D;
        if (tex == null)
        {
            EditorUtility.DisplayDialog("M-Face LocalML", "Select a Texture2D asset.", "OK");
            return;
        }

        string temp = Path.Combine(Application.temporaryCachePath, "mface_localml_test.jpg");
        File.WriteAllBytes(temp, tex.EncodeToJPG());
        RunAnalysis(temp, correctType: 0);
    }

    [MenuItem(MenuRoot + "Reset Local ML Runtime")]
    static void ResetRuntime()
    {
        MFaceLocalEmotionAnalyzer.Reset();
        Debug.Log("[MFaceLocalML] Runtime reset.");
    }

    static void RunAnalysis(string path, int correctType)
    {
        MFaceLocalEmotionAnalyzer.Reset();
        var go = new GameObject("MFaceLocalML_TestRunner");
        var runner = go.AddComponent<TestRunner>();
        runner.path = path;
        runner.correctType = correctType;
    }

    sealed class TestRunner : MonoBehaviour
    {
        public string path;
        public int correctType;

        IEnumerator Start()
        {
            yield return MFaceLocalEmotionAnalyzer.AnalyzeFile(path, correctType, 0.15f, (emotion, em, raw) =>
            {
                string msg = emotion < 0
                    ? "FAILED: " + raw
                    : $"emotion={emotion} ({Label(emotion)})\n{raw}";
                Debug.Log("[MFaceLocalML] Test result:\n" + msg);
                EditorUtility.DisplayDialog("M-Face LocalML", msg, "OK");
                Destroy(gameObject);
            });
        }

        static string Label(int e) =>
            e == 0 ? "Happy" : e == 1 ? "Neutral" : e == 2 ? "Sad" : e == 3 ? "Anger" : "?";
    }
}
#endif
