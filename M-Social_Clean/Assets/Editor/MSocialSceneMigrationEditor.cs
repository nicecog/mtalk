#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public static class MSocialSceneMigrationEditor
{
    static readonly string[] BootstrapNames = {
        "Background", "1. IntroLogo", "2. LoginPage", "3. IntroMovie"
    };

    static readonly string[] Stage1Names = {
        "4. Stage1Start", "5. Stage1Intro", "6. Stage1SelectMap", "7. Stage1SelectScene",
        "8. Stage1WatchScene", "9-1. Stage1Positive", "9-2. Stage1Negative", "9-3. Stage1Feedback",
        "10. Stage1End"
    };

    static readonly string[] Stage2Names = {
        "11. Stage2Start", "12. Stage2Intro", "13. Stage2Select", "14. Stage2WatchScene",
        "15. ActSelect", "16. ResultPositive", "16-2. ResultNegative", "17. PositiveFeedback", "18. EndPage"
    };

    const string MainPath = "Assets/Scenes/MainScene.unity";
    const string LegacyPath = "Assets/Scenes/Legacy/MainScene.unity";
    const string BootstrapPath = "Assets/Scenes/MSocial_Bootstrap.unity";
    const string Stage1Path = "Assets/Scenes/MSocial_Stage1.unity";
    const string Stage2Path = "Assets/Scenes/MSocial_Stage2.unity";

    [MenuItem("M-Social/Migrate To Split Scenes")]
    public static void ExecuteMigrationMenu() => ExecuteMigrationBatch();

    public static void ExecuteMigrationBatch()
    {
        if (!File.Exists(MainPath))
        {
            Debug.LogError("MainScene not found at " + MainPath);
            EditorApplication.Exit(1);
            return;
        }

        Directory.CreateDirectory("Assets/Scenes/Legacy");
        if (!File.Exists(LegacyPath))
            AssetDatabase.CopyAsset(MainPath, LegacyPath);
        else
            File.Copy(LegacyPath, MainPath, true);

        AssetDatabase.Refresh();

        var scene = EditorSceneManager.OpenScene(MainPath, OpenSceneMode.Single);
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("Canvas not found");
            EditorApplication.Exit(1);
            return;
        }

        BuildStageCatalogFromScene();
        TagAllPages(canvas.transform);
        StripIntroMovieRenderTexture(canvas.transform);
        EnsureBootstrapComponents(canvas);

        var stage1Objects = CollectChildren(canvas.transform, Stage1Names);
        var stage2Objects = CollectChildren(canvas.transform, Stage2Names);

        var stage1Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var s1Root = CreateContentRoot(stage1Scene, "MSocial_Stage1_Root");
        foreach (var go in stage1Objects)
            MoveUnderRoot(go, s1Root, stage1Scene);

        EditorSceneManager.SaveScene(stage1Scene, Stage1Path);
        EditorSceneManager.CloseScene(stage1Scene, true);

        var stage2Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var s2Root = CreateContentRoot(stage2Scene, "MSocial_Stage2_Root");
        foreach (var go in stage2Objects)
            MoveUnderRoot(go, s2Root, stage2Scene);

        EditorSceneManager.SaveScene(stage2Scene, Stage2Path);
        EditorSceneManager.CloseScene(stage2Scene, true);

        var bootstrapScene = SceneManager.GetSceneByPath(MainPath);
        if (!bootstrapScene.IsValid())
            bootstrapScene = SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(bootstrapScene, BootstrapPath);

        UpdateBuildSettings();
        PlayerSettings.Android.bundleVersionCode = 5;
        PlayerSettings.bundleVersion = "1.17";
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("MSocial scene migration complete: Bootstrap + Stage1 + Stage2");
    }

    static List<GameObject> CollectChildren(Transform parent, string[] names)
    {
        var list = new List<GameObject>();
        var set = new HashSet<string>(names);
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i).gameObject;
            if (set.Contains(child.name))
                list.Add(child);
        }
        list.Reverse();
        return list;
    }

    static GameObject CreateContentRoot(Scene scene, string rootName)
    {
        var root = new GameObject(rootName);
        root.AddComponent<MSocialSceneContentRoot>();
        SceneManager.MoveGameObjectToScene(root, scene);
        return root;
    }

    static void MoveUnderRoot(GameObject go, GameObject root, Scene targetScene)
    {
        go.transform.SetParent(null, false);
        EditorSceneManager.MoveGameObjectToScene(go, targetScene);
        go.transform.SetParent(root.transform, false);
    }

    static void TagAllPages(Transform canvas)
    {
        for (int i = 0; i < canvas.childCount; i++)
        {
            var child = canvas.GetChild(i).gameObject;
            var id = MSocialPageMarker.FromLegacyName(child.name);
            if (id == MSocialPageId.None)
                continue;
            var marker = child.GetComponent<MSocialPageMarker>();
            if (marker == null)
                marker = child.AddComponent<MSocialPageMarker>();
            marker.pageId = id;
            if (id != MSocialPageId.Background)
                child.tag = "Page";
        }

        foreach (var marker in canvas.GetComponentsInChildren<MSocialPageMarker>(true))
        {
            if (marker.pageId != MSocialPageId.Background)
                marker.gameObject.tag = "Page";
        }
    }

    static void StripIntroMovieRenderTexture(Transform canvas)
    {
        foreach (var vp in canvas.GetComponentsInChildren<VideoPlayer>(true))
        {
            if (vp.gameObject.name.Contains("IntroMovie") || vp.GetComponent<MSocialIntroMoviePlayer>() != null)
            {
                vp.targetTexture = null;
                var raw = vp.GetComponent<RawImage>();
                if (raw != null)
                    raw.texture = null;
            }
        }
    }

    static void EnsureBootstrapComponents(GameObject canvas)
    {
        var bootstrap = canvas.GetComponent<MSocialSceneBootstrap>();
        if (bootstrap == null)
            bootstrap = canvas.AddComponent<MSocialSceneBootstrap>();
        bootstrap.pageContainer = canvas.GetComponent<RectTransform>();
        bootstrap.flowManager = Object.FindFirstObjectByType<FlowManager>(FindObjectsInactive.Include);
        bootstrap.login = Object.FindFirstObjectByType<Login>(FindObjectsInactive.Include);

        var bg = canvas.transform.Find("Background");
        if (bg != null)
        {
            var fm = bg.GetComponent<FlowManager>();
            if (fm != null)
            {
                fm.firstPageId = MSocialPageId.IntroLogo;
                fm.homePageId = MSocialPageId.Stage1Start;
            }
        }
    }

    static void BuildStageCatalogFromScene()
    {
        var s2m = Object.FindFirstObjectByType<Stage2Manager>(FindObjectsInactive.Include);
        var sp = Object.FindFirstObjectByType<ScenePlayer>(FindObjectsInactive.Include);
        if (s2m == null || s2m.FirstFrames == null || s2m.FirstFrames.Length == 0)
        {
            Debug.LogWarning("Stage2Manager.FirstFrames not found — catalog skipped");
            return;
        }

        Directory.CreateDirectory("Assets/Resources");
        var path = "Assets/Resources/MSocialStageCatalog.asset";
        var catalog = AssetDatabase.LoadAssetAtPath<MSocialStageCatalog>(path);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<MSocialStageCatalog>();
            AssetDatabase.CreateAsset(catalog, path);
        }
        catalog.firstFrames = s2m.FirstFrames;
        if (sp != null)
        {
            catalog.titles = sp.titles;
            catalog.videoCodes = sp.VideoCodes;
            catalog.prv = sp.prv;
        }
        EditorUtility.SetDirty(catalog);
    }

    static void UpdateBuildSettings()
    {
        var scenes = new[]
        {
            new EditorBuildSettingsScene(BootstrapPath, true),
            new EditorBuildSettingsScene(Stage1Path, true),
            new EditorBuildSettingsScene(Stage2Path, true)
        };
        EditorBuildSettings.scenes = scenes;
    }
}
#endif
