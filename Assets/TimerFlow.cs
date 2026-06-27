using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TimerFlow : MonoBehaviour
{
    public GameObject SelfPage;
    public GameObject NextPage;
    public string NextPageName;
    public string PackSceneToLoad;

    public float WaitSeconds;
    public int nextBG = 0;
    public bool nextBG_alpha = true;

    FlowManager fm;

    void Start()
    {
        fm = FindFirstObjectByType<FlowManager>();
    }

    void OnEnable()
    {
        StartCoroutine(WaitForNextScene());
    }

    IEnumerator WaitForNextScene()
    {
        MBodyDiagLog.Step("TimerFlow", $"Wait start on '{name}' seconds={WaitSeconds}");
        yield return new WaitForSecondsRealtime(WaitSeconds);

        var next = ResolveNextPage();
        if (next == null)
        {
            MBodyDiagLog.Error("TimerFlow", $"No next page from '{name}' nextName='{NextPageName}'");
            yield break;
        }

        MBodyDiagLog.Step("TimerFlow", $"Navigate '{name}' -> {next.name}");

        var pack = PackSceneToLoad;
        if (string.IsNullOrEmpty(pack) && next.scene != gameObject.scene)
            pack = next.scene.name;

        if (!string.IsNullOrEmpty(pack) && !ScenePackLoader.IsLoaded(pack))
            yield return ScenePackLoader.EnsureLoaded(pack);

        SceneFlowNavigator.ShowOnly(next, SelfPage);

        if (fm != null)
        {
            fm.setBackground(nextBG);
            var useAlpha = nextBG_alpha && next.name != "MBodyLogin";
            fm.setBlackScreen(useAlpha);
        }
    }

    GameObject ResolveNextPage()
    {
        if (NextPage != null)
            return NextPage;

        if (!string.IsNullOrEmpty(NextPageName))
            return SceneFlowRegistry.Get(NextPageName);

        return null;
    }
}
