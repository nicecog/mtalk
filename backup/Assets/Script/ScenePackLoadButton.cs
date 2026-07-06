using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to a UI button: loads an additive scene pack, then runs ButtonFlow.ClickNext().
/// </summary>
public sealed class ScenePackLoadButton : MonoBehaviour
{
    [SerializeField] string packSceneName;
    [SerializeField] ButtonFlow buttonFlow;

    public void OnClickLoadThenNext()
    {
        StartCoroutine(LoadThenNext());
    }

    IEnumerator LoadThenNext()
    {
        if (!string.IsNullOrEmpty(packSceneName))
            yield return ScenePackLoader.EnsureLoaded(packSceneName);

        if (buttonFlow != null)
            buttonFlow.ClickNext();
    }
}
