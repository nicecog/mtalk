#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor Play Mode: logs active SceneFlow pages every few seconds for MCP/logcat checks.
/// </summary>
[InitializeOnLoad]
static class FlowPlayModeDiagnostics
{
    const string LogTag = "[FlowDiag]";
    const double IntervalSec = 3.0;

    static double _nextLogTime;

    static FlowPlayModeDiagnostics()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += OnEditorUpdate;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            _nextLogTime = EditorApplication.timeSinceStartup + 1.0;
            Debug.Log($"{LogTag} Play Mode entered — monitoring SceneFlow pages.");
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            Debug.Log($"{LogTag} Play Mode exiting.");
        }
    }

    static void OnEditorUpdate()
    {
        if (!EditorApplication.isPlaying)
            return;

        if (EditorApplication.timeSinceStartup < _nextLogTime)
            return;

        _nextLogTime = EditorApplication.timeSinceStartup + IntervalSec;
        LogActiveSceneFlowPages();
    }

    static void LogActiveSceneFlowPages()
    {
        var pages = GameObject.FindGameObjectsWithTag("SceneFlow");
        var active = new List<string>();
        foreach (var page in pages)
        {
            if (page.activeInHierarchy)
                active.Add(page.name);
        }

        active.Sort();
        var summary = active.Count == 0 ? "(none)" : string.Join(", ", active.Distinct());
        Debug.Log($"{LogTag} active SceneFlow ({active.Count}): {summary}");

        var fm = Object.FindFirstObjectByType<FlowManager>();
        if (fm != null && fm.FirstPage != null)
            Debug.Log($"{LogTag} FlowManager.FirstPage={fm.FirstPage.name}, level={fm.level}");
    }
}
#endif
