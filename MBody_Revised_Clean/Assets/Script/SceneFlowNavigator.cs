using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Ensures only one SceneFlow page is visible and updates FlowManager side effects.
/// </summary>
public static class SceneFlowNavigator
{
    public static void ShowOnly(GameObject next, GameObject selfPage)
    {
        if (next == null)
        {
            MBodyDiagLog.Warn("Navigate", "ShowOnly called with null next page");
            return;
        }

        var flowPage = ResolveFlowPage(next, selfPage);
        if (flowPage == null)
        {
            MBodyDiagLog.Warn("Navigate", $"ShowOnly could not resolve page from '{next.name}'");
            return;
        }

        var selfName = selfPage != null ? selfPage.name : "(null)";
        if (flowPage != next)
            MBodyDiagLog.Step("Navigate", $"ShowOnly {selfName} -> {flowPage.name} (resolved from {next.name})");
        else
            MBodyDiagLog.Step("Navigate", $"ShowOnly {selfName} -> {flowPage.name} (id={flowPage.GetInstanceID()}) parent={Describe(flowPage.transform)}");

        BodyResourceLifecycle.PrepareForPageTransition();

        var pages = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (var i = 0; i < pages.Length; i++)
        {
            var page = pages[i];
            if (page == null || !page.CompareTag("SceneFlow") || page == flowPage)
                continue;

            HideSceneFlowPage(page);
        }

        DeactivateSameNameDuplicates(flowPage);

        if (selfPage != null && selfPage != flowPage)
            HideSceneFlowPage(selfPage);

        EnsureHierarchyActive(flowPage);
        flowPage.SetActive(true);

        var fm = Object.FindFirstObjectByType<FlowManager>();
        if (fm != null)
            fm.OnSceneFlowPageShown(flowPage);
    }

    static void HideSceneFlowPage(GameObject page)
    {
        if (page == null || !page.activeSelf)
            return;

        StopPageMedia(page);
        page.SetActive(false);
    }

    static void DeactivateSameNameDuplicates(GameObject keep)
    {
        if (keep == null)
            return;

        var pages = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (var i = 0; i < pages.Length; i++)
        {
            var page = pages[i];
            if (page == null || page == keep || !page.CompareTag("SceneFlow"))
                continue;
            if (!string.Equals(page.name, keep.name, System.StringComparison.Ordinal))
                continue;

            if (page.activeSelf) {
                MBodyDiagLog.Step("Navigate",
                    $"Deactivated duplicate SceneFlow '{page.name}' id={page.GetInstanceID()} parent={Describe(page.transform)}");
                StopPageMedia(page);
                page.SetActive(false);
            }
        }
    }

    static void StopPageMedia(GameObject page)
    {
        if (page == null)
            return;

        var players = page.GetComponentsInChildren<VideoPlayer>(true);
        for (var i = 0; i < players.Length; i++)
        {
            var vp = players[i];
            if (vp != null && vp.isPlaying)
                vp.Stop();
        }

        var sources = page.GetComponentsInChildren<AudioSource>(true);
        for (var i = 0; i < sources.Length; i++)
        {
            var source = sources[i];
            if (source != null && source.isPlaying)
                source.Stop();
        }
    }

    static GameObject ResolveFlowPage(GameObject next, GameObject selfPage)
    {
        if (next.CompareTag("SceneFlow"))
            return next;

        // Legacy buttons on EndSelect/UserPop point at overlay widgets, not SceneFlow pages.
        if (string.Equals(next.name, "Review", System.StringComparison.Ordinal))
        {
            var bodyReview = Object.FindFirstObjectByType<BodyReview>(FindObjectsInactive.Include);
            if (bodyReview != null)
                return bodyReview.gameObject;

            var videoReview = Object.FindFirstObjectByType<VideoReview>(FindObjectsInactive.Include);
            if (videoReview != null)
                return videoReview.gameObject;
        }

        if (string.Equals(next.name, "Result", System.StringComparison.Ordinal))
        {
            var bodyResult = FindBodyResultPage();
            if (bodyResult != null)
                return bodyResult;
        }

        var registered = SceneFlowRegistry.Get(next.name);
        return registered != null ? registered : next;
    }

    static GameObject FindBodyResultPage()
    {
        var csv = Object.FindFirstObjectByType<CSVScroll>(FindObjectsInactive.Include);
        if (csv != null)
        {
            var t = csv.transform;
            while (t != null)
            {
                if (t.CompareTag("SceneFlow"))
                    return t.gameObject;
                t = t.parent;
            }
        }

        return SceneFlowRegistry.Get("Result");
    }

    static void EnsureHierarchyActive(GameObject go)
    {
        for (var t = go.transform; t != null; t = t.parent)
        {
            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);
        }
    }

    static string Describe(Transform t)
    {
        if (t == null)
            return "(null)";
        return t.parent != null ? $"{t.parent.name}/{t.name}" : t.name;
    }
}
