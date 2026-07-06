using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LoginSceneSplitSetup
{
    const string LoginScenePath = "Assets/Scenes/Login.unity";
    const string MainScenePath = "Assets/Scenes/MBody.unity";
    const string BodyPackPath = "Assets/Scenes/MBodyBodyPack.unity";
    const string DancePackPath = "Assets/Scenes/MBodyDancePack.unity";
    const string BodyPackSceneName = "MBodyBodyPack";
    const string DancePackSceneName = "MBodyDancePack";

    static readonly HashSet<string> BodyPages = new HashSet<string>
    {
        "MusicSelect", "SoundSelectYellow", "SoundSelectBlue", "SpeedSelect", "SpeedSelectPop",
        "MainGameCali", "ReadyMessage", "MainGame", "EndMessage", "Alert"
    };

    static readonly HashSet<string> DancePages = new HashSet<string>
    {
        "DanceSelect", "DanceLevelSelect", "LV1", "LV1End", "LV2", "LV2End",
        "LV3Intro", "LV3Preview", "LV3Calib", "LV3Play", "LV3End", "LV3ThirdEnd",
        "LV4MusicSelect", "LV4Next", "LV4Cali", "LV4Game", "LV4End", "PoseGuide", "PoseGuidePost"
    };

    [MenuItem("MBody/Setup/Create Login + Scene Packs (shell)")]
    public static void CreateShellScenes()
    {
        CreateEmptyScene(LoginScenePath, "Login");
        CreateEmptyScene(BodyPackPath, BodyPackSceneName);
        CreateEmptyScene(DancePackPath, DancePackSceneName);
        AddSceneToBuildSettings(LoginScenePath);
        AddSceneToBuildSettings(BodyPackPath);
        AddSceneToBuildSettings(DancePackPath);
        AssetDatabase.SaveAssets();
        Debug.Log("[LoginSceneSplitSetup] Shell scenes created.");
    }

    class FlowWire
    {
        public ButtonFlow Button;
        public TimerFlow Timer;
        public string SelfName;
        public string NextName;
    }

    [MenuItem("MBody/Setup/Split Body and Dance Scene Packs")]
    public static void SplitScenePacks()
    {
        EditorSceneManager.SaveOpenScenes();
        try
        {
            CreateShellScenes();

            var mainScene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var bodyScene = EditorSceneManager.OpenScene(BodyPackPath, OpenSceneMode.Additive);
            var danceScene = EditorSceneManager.OpenScene(DancePackPath, OpenSceneMode.Additive);

            EnsureSceneRoot(bodyScene, BodyPackSceneName);
            EnsureSceneRoot(danceScene, DancePackSceneName);

            var wires = CaptureFlowWires();

            var pages = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(go => go.CompareTag("SceneFlow"))
                .ToList();

            var movedBody = 0;
            var movedDance = 0;
            foreach (var page in pages.ToList())
            {
                if (page.scene != mainScene)
                    continue;

                if (BodyPages.Contains(page.name))
                {
                    MovePage(page, bodyScene);
                    movedBody++;
                }
                else if (DancePages.Contains(page.name))
                {
                    MovePage(page, danceScene);
                    movedDance++;
                }
            }

            ApplyFlowWires(wires);
            WireTrainingButtons();
            FixFlowManagerPages();
            AddDanceModePages(danceScene);

            EditorSceneManager.MarkSceneDirty(mainScene);
            EditorSceneManager.MarkSceneDirty(bodyScene);
            EditorSceneManager.MarkSceneDirty(danceScene);
            EditorSceneManager.SaveScene(mainScene);
            EditorSceneManager.SaveScene(bodyScene);
            EditorSceneManager.SaveScene(danceScene);

            Debug.Log($"[LoginSceneSplitSetup] Split complete. Body={movedBody}, Dance={movedDance}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[LoginSceneSplitSetup] Split failed: " + ex);
            throw;
        }
    }

    static List<FlowWire> CaptureFlowWires()
    {
        var wires = new List<FlowWire>();
        foreach (var flow in Object.FindObjectsByType<ButtonFlow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (flow.SelfPage == null || flow.NextPage == null)
                continue;
            wires.Add(new FlowWire
            {
                Button = flow,
                SelfName = flow.SelfPage.name,
                NextName = flow.NextPage.name
            });
        }

        foreach (var flow in Object.FindObjectsByType<TimerFlow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (flow.SelfPage == null || flow.NextPage == null)
                continue;
            wires.Add(new FlowWire
            {
                Timer = flow,
                SelfName = flow.SelfPage.name,
                NextName = flow.NextPage.name
            });
        }

        return wires;
    }

    static void ApplyFlowWires(List<FlowWire> wires)
    {
        foreach (var wire in wires)
        {
            var pack = PackSceneForPage(wire.NextName);
            if (wire.Button != null)
            {
                wire.Button.NextPageName = wire.NextName;
                wire.Button.PackSceneToLoad = pack;
                wire.Button.NextPage = null;
                EditorUtility.SetDirty(wire.Button);
            }
            if (wire.Timer != null)
            {
                wire.Timer.NextPageName = wire.NextName;
                wire.Timer.PackSceneToLoad = pack;
                wire.Timer.NextPage = null;
                EditorUtility.SetDirty(wire.Timer);
            }
        }
    }

    static string PackSceneForPage(string pageName)
    {
        if (BodyPages.Contains(pageName))
            return BodyPackSceneName;
        if (DancePages.Contains(pageName))
            return DancePackSceneName;
        return string.Empty;
    }

    static void FixFlowManagerPages()
    {
        foreach (var fm in Object.FindObjectsByType<FlowManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            EditorUtility.SetDirty(fm);
    }

    static void MovePage(GameObject page, Scene targetScene)
    {
        if (page.scene == targetScene)
            return;

        var packRoot = targetScene.GetRootGameObjects().FirstOrDefault(r => r.name.Contains("Pack"));
        if (page.transform.parent != null)
            page.transform.SetParent(null, false);

        EditorSceneManager.MoveGameObjectToScene(page, targetScene);

        if (packRoot != null)
            page.transform.SetParent(packRoot.transform, false);
    }

    static void EnsureSceneRoot(Scene scene, string rootName)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == rootName)
                return;
        }

        var go = new GameObject(rootName);
        EditorSceneManager.MoveGameObjectToScene(go, scene);
    }

    static void WireTrainingButtons()
    {
        foreach (var flow in Object.FindObjectsByType<ButtonFlow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (flow.gameObject.name == "BodyTraining")
            {
                flow.PackSceneToLoad = BodyPackSceneName;
                EditorUtility.SetDirty(flow);
            }
            else if (flow.gameObject.name == "DanceTraining")
            {
                flow.PackSceneToLoad = DancePackSceneName;
                EditorUtility.SetDirty(flow);
            }
        }
    }

    static void AddDanceModePages(Scene danceScene)
    {
        foreach (var root in danceScene.GetRootGameObjects())
        {
            var pages = root.GetComponentsInChildren<Transform>(true)
                .Select(t => t.gameObject)
                .Where(go => go.CompareTag("SceneFlow"));
            foreach (var page in pages)
            {
                if (page.GetComponent<DanceModePage>() == null)
                    page.AddComponent<DanceModePage>();
            }
        }
    }

    static void CreateEmptyScene(string path, string rootName)
    {
        if (File.Exists(path))
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject(rootName);
        if (rootName == "Login")
        {
            root.AddComponent<LoginSceneController>();
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        EditorSceneManager.SaveScene(scene, path);
    }

    static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Any(s => s.path == scenePath))
            return;

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
