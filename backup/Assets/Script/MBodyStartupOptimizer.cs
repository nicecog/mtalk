using System.Collections;
using UnityEngine;

/// <summary>
/// Trims unused assets after the main scene finishes its first-frame setup.
/// </summary>
public sealed class MBodyStartupOptimizer : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return null;
        SceneFlowRegistry.Refresh();
        yield return Resources.UnloadUnusedAssets();
    }
}
