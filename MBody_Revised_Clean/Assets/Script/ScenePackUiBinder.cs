using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reparents additive pack UI (SceneFlow pages) under the core MBody Canvas/Scenes root.
/// Pack scenes were split without a Canvas, so their UI does not render until attached.
/// </summary>
public static class ScenePackUiBinder
{
    static readonly HashSet<Scene> Attached = new HashSet<Scene>();

    public static void AttachPackUi(Scene packScene)
    {
        if (!packScene.IsValid() || !packScene.isLoaded || Attached.Contains(packScene))
            return;

        var scenesRoot = ResolveScenesRoot();
        if (scenesRoot == null)
        {
            MBodyDiagLog.Error("PackUi", $"Scenes root not found while attaching '{packScene.name}'");
            return;
        }

        var moved = 0;
        foreach (var root in packScene.GetRootGameObjects())
        {
            if (!root.name.Contains("Pack"))
                continue;

            moved += ReparentUiChildren(root.transform, scenesRoot);
        }

        Attached.Add(packScene);
        MBodyDiagLog.Step("PackUi", $"Attached '{packScene.name}' pages={moved} -> {scenesRoot.name}");
    }

    static Transform ResolveScenesRoot()
    {
        var fm = Object.FindFirstObjectByType<FlowManager>();
        if (fm != null)
            return fm.transform;

        var scenes = GameObject.Find("Scenes");
        return scenes != null ? scenes.transform : null;
    }

    static int ReparentUiChildren(Transform packRoot, Transform scenesRoot)
    {
        var toMove = new List<Transform>();
        for (var i = 0; i < packRoot.childCount; i++)
        {
            var child = packRoot.GetChild(i);
            if (child is RectTransform)
                toMove.Add(child);
        }

        for (var i = 0; i < toMove.Count; i++)
        {
            var child = toMove[i];
            if (child.CompareTag("SceneFlow")) {
                var existing = FindDirectSceneFlowChild(scenesRoot, child.name);
                if (existing != null && existing != child) {
                    MBodyDiagLog.Warn("PackUi",
                        $"Replacing duplicate SceneFlow stub '{child.name}' id={existing.gameObject.GetInstanceID()} with pack page id={child.gameObject.GetInstanceID()}");
                    Object.Destroy(existing.gameObject);
                }
            }

            child.SetParent(scenesRoot, false);
            child.gameObject.SetActive(false);
            MBodyDiagLog.Step("PackUi", $"Reparented '{child.name}' scene={child.gameObject.scene.name}");
        }

        return toMove.Count;
    }

    static Transform FindDirectSceneFlowChild(Transform scenesRoot, string pageName)
    {
        for (var i = 0; i < scenesRoot.childCount; i++) {
            var child = scenesRoot.GetChild(i);
            if (child.name == pageName && child.CompareTag("SceneFlow"))
                return child;
        }

        return null;
    }
}
