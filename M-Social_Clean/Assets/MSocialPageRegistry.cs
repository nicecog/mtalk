using System.Collections.Generic;
using UnityEngine;

public static class MSocialPageRegistry
{
    static readonly Dictionary<MSocialPageId, GameObject> Pages = new Dictionary<MSocialPageId, GameObject>();

    public static void Register(MSocialPageId id, GameObject go)
    {
        if (id == MSocialPageId.None || go == null)
            return;
        Pages[id] = go;
    }

    public static void Unregister(MSocialPageId id, GameObject go)
    {
        if (id == MSocialPageId.None)
            return;
        if (Pages.TryGetValue(id, out var current) && current == go)
            Pages.Remove(id);
    }

    public static GameObject Get(MSocialPageId id)
    {
        if (id == MSocialPageId.None)
            return null;
        Pages.TryGetValue(id, out var go);
        return go;
    }

    public static void DeactivateAllExcept(MSocialPageId activeId)
    {
        EnsureRegistered();
        foreach (var kv in Pages)
        {
            if (kv.Value == null)
                continue;
            if (kv.Key == MSocialPageId.Background)
                continue;
            kv.Value.SetActive(kv.Key == activeId);
        }
    }

    public static void ScanAndRegister(Transform root)
    {
        if (root == null)
            return;
        foreach (var marker in root.GetComponentsInChildren<MSocialPageMarker>(true))
            Register(marker.pageId, marker.gameObject);
    }

    public static void EnsureRegistered()
    {
        if (Pages.Count > 0)
            return;
        Refresh();
    }

    public static void Refresh()
    {
        if (_pageContainer != null)
            ScanAndRegister(_pageContainer);
        foreach (var marker in Object.FindObjectsByType<MSocialPageMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Register(marker.pageId, marker.gameObject);
    }

    static Transform _pageContainer;

    public static void SetPageContainer(Transform container)
    {
        _pageContainer = container;
    }
}
