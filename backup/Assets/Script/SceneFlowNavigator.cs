using UnityEngine;



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



        var selfName = selfPage != null ? selfPage.name : "(null)";

        MBodyDiagLog.Step("Navigate", $"ShowOnly {selfName} -> {next.name} (id={next.GetInstanceID()}) parent={Describe(next.transform)}");



        var pages = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (var i = 0; i < pages.Length; i++)

        {

            var page = pages[i];

            if (page == null || !page.CompareTag("SceneFlow") || page == next)

                continue;



            page.SetActive(false);

        }



        if (selfPage != null && selfPage != next)

            selfPage.SetActive(false);



        next.SetActive(true);



        var fm = Object.FindFirstObjectByType<FlowManager>();

        if (fm != null)

            fm.OnSceneFlowPageShown(next);

    }



    static string Describe(Transform t)

    {

        if (t == null)

            return "(null)";

        return t.parent != null ? $"{t.parent.name}/{t.name}" : t.name;

    }

}

