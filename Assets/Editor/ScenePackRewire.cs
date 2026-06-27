using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ScenePackRewire
{
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

    [MenuItem("MBody/Setup/Re-wire Cross-Scene Flows")]
    public static void RewireCrossSceneFlows()
    {
        EditorSceneManager.SaveOpenScenes();
        var mainScene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        var bodyScene = EditorSceneManager.OpenScene(BodyPackPath, OpenSceneMode.Additive);
        var danceScene = EditorSceneManager.OpenScene(DancePackPath, OpenSceneMode.Additive);

        var flowManager = FindInScene<FlowManager>(mainScene);
        var bodyManager = FindInScene<BodyManager>(mainScene);
        var danceManager = FindInScene<DanceManager>(mainScene);
        var loginForm = FindInScene<LoginForm>(mainScene);
        var videoCapture = FindInScene<VideoCaptureCam>(mainScene);
        var bodyProxy = EnsureProxy(bodyScene);
        var danceProxy = EnsureProxy(danceScene);

        var flowFixed = RewireFlows();
        var buttonFixed = RewireButtons(flowManager, bodyManager, danceManager, loginForm, videoCapture, bodyProxy, danceProxy);
        FixFlowManagerPages(flowManager);
        WireTrainingButtons();

        EditorSceneManager.MarkSceneDirty(mainScene);
        EditorSceneManager.MarkSceneDirty(bodyScene);
        EditorSceneManager.MarkSceneDirty(danceScene);
        EditorSceneManager.SaveScene(mainScene);
        EditorSceneManager.SaveScene(bodyScene);
        EditorSceneManager.SaveScene(danceScene);

        Debug.Log($"[ScenePackRewire] Flow fixes={flowFixed}, Button fixes={buttonFixed}");
    }

    static int RewireFlows()
    {
        var count = 0;
        foreach (var flow in UnityEngine.Object.FindObjectsByType<ButtonFlow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (FixFlow(flow))
                count++;
        }

        foreach (var flow in UnityEngine.Object.FindObjectsByType<TimerFlow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (FixFlow(flow))
                count++;
        }

        return count;
    }

    static bool FixFlow(ButtonFlow flow)
    {
        var changed = false;
        if (flow.SelfPage == null)
        {
            var page = FindSceneFlowAncestor(flow.transform);
            if (page != null)
            {
                flow.SelfPage = page;
                changed = true;
            }
        }

        if (flow.NextPage != null && flow.SelfPage != null && flow.NextPage.scene != flow.SelfPage.scene)
        {
            flow.NextPageName = flow.NextPage.name;
            flow.PackSceneToLoad = PackSceneForPage(flow.NextPageName);
            flow.NextPage = null;
            changed = true;
        }
        else if (flow.NextPage == null && string.IsNullOrEmpty(flow.NextPageName))
        {
            var inferred = InferNextPageName(flow);
            if (!string.IsNullOrEmpty(inferred))
            {
                flow.NextPageName = inferred;
                flow.PackSceneToLoad = PackSceneForPage(inferred);
                changed = true;
            }
        }

        if (changed)
            EditorUtility.SetDirty(flow);

        return changed;
    }

    static bool FixFlow(TimerFlow flow)
    {
        var changed = false;
        if (flow.SelfPage == null)
        {
            var page = FindSceneFlowAncestor(flow.transform);
            if (page != null)
            {
                flow.SelfPage = page;
                changed = true;
            }
        }

        if (flow.NextPage != null && flow.SelfPage != null && flow.NextPage.scene != flow.SelfPage.scene)
        {
            flow.NextPageName = flow.NextPage.name;
            flow.PackSceneToLoad = PackSceneForPage(flow.NextPageName);
            flow.NextPage = null;
            changed = true;
        }

        if (changed)
            EditorUtility.SetDirty(flow);

        return changed;
    }

    static string InferNextPageName(ButtonFlow flow)
    {
        var host = flow.gameObject.name;
        if (host == "menu" || host == "logout")
            return "BodyDanceSelect";
        if (host.StartsWith("SkipButton", StringComparison.Ordinal))
            return "BodyDanceSelect";
        if (host == "BodyTraining")
            return "IntroVideo";
        if (host == "DanceTraining")
            return "IntroVideo";
        return string.Empty;
    }

    static PackServiceProxy EnsureProxy(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var proxy = root.GetComponentInChildren<PackServiceProxy>(true);
            if (proxy != null)
                return proxy;
        }

        var packRoot = scene.GetRootGameObjects().FirstOrDefault(r => r.name.Contains("Pack"));
        if (packRoot == null)
            packRoot = new GameObject(scene.name);
        var created = packRoot.GetComponent<PackServiceProxy>();
        if (created == null)
            created = packRoot.AddComponent<PackServiceProxy>();
        return created;
    }

    static int RewireButtons(
        FlowManager flowManager,
        BodyManager bodyManager,
        DanceManager danceManager,
        LoginForm loginForm,
        VideoCaptureCam videoCapture,
        PackServiceProxy bodyProxy,
        PackServiceProxy danceProxy)
    {
        var count = 0;
        foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var page = FindSceneFlowAncestor(button.transform);
            var so = new SerializedObject(button);
            var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            if (calls == null || !calls.isArray)
                continue;

            for (var i = 0; i < calls.arraySize; i++)
            {
                var call = calls.GetArrayElementAtIndex(i);
                var target = call.FindPropertyRelative("m_Target");
                if (target == null || target.objectReferenceValue != null)
                    continue;

                var typeName = call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue;
                var method = call.FindPropertyRelative("m_MethodName").stringValue;
                var boolArg = call.FindPropertyRelative("m_Arguments.m_BoolArgument").boolValue;

                UnityEngine.Object resolved = null;
                var proxy = button.gameObject.scene.name == BodyPackSceneName ? bodyProxy : danceProxy;
                if (typeName.Contains("FlowManager") && proxy != null)
                    resolved = RedirectToProxy(proxy, method);
                else if (typeName.Contains("FlowManager") && flowManager != null && button.gameObject.scene == flowManager.gameObject.scene)
                    resolved = flowManager;
                else if (typeName.Contains("BodyManager") && proxy != null)
                    resolved = RedirectToProxy(proxy, method);
                else if (typeName.Contains("BodyManager") && bodyManager != null && button.gameObject.scene == bodyManager.gameObject.scene)
                    resolved = bodyManager;
                else if (typeName.Contains("DanceManager") && proxy != null)
                    resolved = RedirectToProxy(proxy, method);
                else if (typeName.Contains("DanceManager") && danceManager != null && button.gameObject.scene == danceManager.gameObject.scene)
                    resolved = danceManager;
                else if (typeName.Contains("LoginForm") && proxy != null)
                    resolved = RedirectToProxy(proxy, method);
                else if (typeName.Contains("LoginForm") && loginForm != null && button.gameObject.scene == loginForm.gameObject.scene)
                    resolved = loginForm;
                else if (typeName.Contains("VideoCaptureCam") && proxy != null)
                    resolved = RedirectToProxy(proxy, method);
                else if (typeName.Contains("VideoCaptureCam") && videoCapture != null)
                    resolved = videoCapture;
                else if (typeName.Contains("UnityEngine.GameObject"))
                    resolved = proxy != null && method == "SetActive"
                        ? proxy
                        : ResolveGameObjectTarget(button, page, flowManager, boolArg);
                else if (typeName.Contains("UnityEngine.Behaviour"))
                    resolved = ResolveBehaviourTarget(page, button, boolArg);
                else if (typeName.Contains("UnityEngine.AudioSource") && page != null)
                    resolved = page.GetComponent<AudioSource>();

                if (resolved == null)
                    continue;

                if (resolved is PackServiceProxy && method == "SetActive")
                    call.FindPropertyRelative("m_MethodName").stringValue = "SetWebCamActive";

                target.objectReferenceValue = resolved;
                count++;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(button);
        }

        return count;
    }

    static PackServiceProxy RedirectToProxy(PackServiceProxy proxy, string method)
    {
        return proxy;
    }

    static UnityEngine.Object ResolveGameObjectTarget(Button button, GameObject page, FlowManager flowManager, bool activate)
    {
        if (flowManager != null && flowManager.WebCamObject != null)
        {
            if (button.name is "menu" or "logout" or "SkipButton" or "NextButton")
                return flowManager.WebCamObject.gameObject;
        }

        if (page != null)
        {
            if (button.name == "Level2")
            {
                var lv2 = page.transform.Find("LV2");
                if (lv2 != null)
                    return lv2.gameObject;
            }

            foreach (var child in page.GetComponentsInChildren<Transform>(true))
            {
                if (child.name is "IntroVideo" or "WebCam" or "Camera")
                    return child.gameObject;
            }
        }

        return null;
    }

    static UnityEngine.Object ResolveBehaviourTarget(GameObject page, Button button, bool enable)
    {
        if (page == null)
            return null;

        var training = page.GetComponentInChildren<BodyGameTraining>(true);
        if (training != null && training.Effect != null)
            return training.Effect;

        var bodyScene = page.GetComponentInChildren<BodyGameScene>(true);
        if (bodyScene != null && bodyScene.Alert != null)
            return bodyScene.Alert;

        var poseGuide = page.GetComponentInChildren<PoseGuide>(true);
        if (poseGuide != null && poseGuide.HalfCam != null)
            return poseGuide.HalfCam;

        var audio = page.GetComponent<AudioSource>();
        if (audio != null)
            return audio;

        foreach (var behaviour in page.GetComponentsInChildren<Behaviour>(true))
        {
            if (behaviour is Button || behaviour is ButtonFlow || behaviour is TimerFlow)
                continue;
            if (behaviour is Image || behaviour is RawImage || behaviour is AudioSource)
                return behaviour;
        }

        return null;
    }

    static void FixFlowManagerPages(FlowManager flowManager)
    {
        if (flowManager == null)
            return;

        if (flowManager.BodyPage == null)
            flowManager.BodyPage = FindPage("MusicSelect");
        if (flowManager.DancePage == null)
            flowManager.DancePage = FindPage("DanceSelect");

        EditorUtility.SetDirty(flowManager);
    }

    static void WireTrainingButtons()
    {
        foreach (var flow in UnityEngine.Object.FindObjectsByType<ButtonFlow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (flow.gameObject.name == "BodyTraining")
            {
                flow.PackSceneToLoad = BodyPackSceneName;
                flow.NextPageName = string.IsNullOrEmpty(flow.NextPageName) ? "IntroVideo" : flow.NextPageName;
                EditorUtility.SetDirty(flow);
            }
            else if (flow.gameObject.name == "DanceTraining")
            {
                flow.PackSceneToLoad = DancePackSceneName;
                flow.NextPageName = string.IsNullOrEmpty(flow.NextPageName) ? "IntroVideo" : flow.NextPageName;
                EditorUtility.SetDirty(flow);
            }
        }
    }

    static GameObject FindPage(string pageName)
    {
        foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go.CompareTag("SceneFlow") && go.name == pageName)
                return go;
        }

        return null;
    }

    static GameObject FindSceneFlowAncestor(Transform t)
    {
        while (t != null)
        {
            if (t.CompareTag("SceneFlow"))
                return t.gameObject;
            t = t.parent;
        }

        return null;
    }

    static T FindInScene<T>(Scene scene) where T : UnityEngine.Object
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = root.GetComponentInChildren<T>(true);
            if (found != null)
                return found;
        }

        return null;
    }

    static string PackSceneForPage(string pageName)
    {
        if (BodyPages.Contains(pageName))
            return BodyPackSceneName;
        if (DancePages.Contains(pageName))
            return DancePackSceneName;
        return string.Empty;
    }
}
