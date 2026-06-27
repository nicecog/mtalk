using System.Collections;

using UnityEngine;

using UnityEngine.SceneManagement;



public class ButtonFlow : MonoBehaviour
{
    public GameObject SelfPage;
    public GameObject NextPage;
    public string NextPageName;
    public string PackSceneToLoad;

    static bool navigating;

    public void ClickNext()
    {
        if (navigating)
            return;

        StartCoroutine(ClickNextRoutine());
    }

    IEnumerator ClickNextRoutine()
    {
        if (navigating)
            yield break;

        navigating = true;
        try
        {
            var next = ResolveNextPage();
            if (next == null)
            {
                MBodyDiagLog.Error("ButtonFlow", $"No next page from '{name}' self='{SelfPage?.name}' nextName='{NextPageName}'");
                yield break;
            }

            MBodyDiagLog.Step("ButtonFlow", $"ClickNext '{name}' -> {next.name} (id={next.GetInstanceID()}) pack='{PackSceneToLoad}'");

            var pack = PackSceneToLoad;
            if (string.IsNullOrEmpty(pack) && next.scene != gameObject.scene)
                pack = next.scene.name;

            if (!string.IsNullOrEmpty(pack) && !ScenePackLoader.IsLoaded(pack))
                yield return ScenePackLoader.EnsureLoaded(pack);

            Navigate(next);
        }
        finally
        {
            navigating = false;
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



    GameObject ResolveSelfPage()

    {

        return SelfPage != null ? SelfPage : null;

    }



    void Navigate(GameObject next)

    {

        SceneFlowNavigator.ShowOnly(next, ResolveSelfPage());

    }

}

