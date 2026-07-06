using System.Collections.Generic;

using UnityEngine;

using UnityEngine.SceneManagement;



/// <summary>

/// Resolves SceneFlow pages by name across the active scene and additive packs.

/// </summary>

public static class SceneFlowRegistry

{

    static readonly Dictionary<string, GameObject> Pages = new Dictionary<string, GameObject>();



    public static void Refresh()

    {

        Pages.Clear();

        var pages = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (var i = 0; i < pages.Length; i++)

        {

            var go = pages[i];

            if (go != null && go.CompareTag("SceneFlow"))

                RegisterPage(go);

        }

    }



    static void RegisterPage(GameObject go)

    {

        if (Pages.TryGetValue(go.name, out var existing) && existing != go)

        {

            MBodyDiagLog.Warn("Registry", $"Duplicate SceneFlow name '{go.name}' ids {existing.GetInstanceID()} -> {go.GetInstanceID()} parents {Describe(existing.transform)} -> {Describe(go.transform)}");

            if (!ShouldPreferPage(go, existing))

                return;

        }



        Pages[go.name] = go;

    }



    /// <summary>

    /// Prefer reparented pack pages under Scenes over BodyTraining/DanceTraining stubs.

    /// </summary>

    static bool ShouldPreferPage(GameObject candidate, GameObject incumbent)

    {

        return PageRegistryScore(candidate) >= PageRegistryScore(incumbent);

    }



    static int PageRegistryScore(GameObject go)

    {

        if (go == null)

            return -1;



        var score = 0;

        var parent = go.transform.parent;

        if (parent != null)

        {

            var parentName = parent.name;

            if (parentName == "Scenes")

                score += 10;

            else if (parentName == "BodyTraining" || parentName == "DanceTraining")

                score -= 5;

        }



        if (go.activeInHierarchy)

            score += 1;



        return score;

    }



    static string Describe(Transform t)

    {

        if (t == null)

            return "(null)";

        return t.parent != null ? $"{t.parent.name}/{t.name}" : t.name;

    }



    public static GameObject Get(string pageName)

    {

        if (string.IsNullOrEmpty(pageName))

            return null;



        if (Pages.Count == 0)

            Refresh();



        GameObject page;

        if (Pages.TryGetValue(pageName, out page))

            return page;



        Refresh();

        return Pages.TryGetValue(pageName, out page) ? page : null;

    }



    public static void RegisterScene(Scene scene)

    {

        if (!scene.IsValid() || !scene.isLoaded)

            return;



        foreach (var root in scene.GetRootGameObjects())

            RegisterTree(root);

    }



    static void RegisterTree(GameObject go)

    {

        if (go.CompareTag("SceneFlow"))

            RegisterPage(go);

        for (var i = 0; i < go.transform.childCount; i++)

            RegisterTree(go.transform.GetChild(i).gameObject);

    }

}

