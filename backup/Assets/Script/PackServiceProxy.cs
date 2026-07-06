using UnityEngine;

/// <summary>
/// Lives in additive pack scenes; forwards UI calls to managers in the core MBody scene.
/// </summary>
public class PackServiceProxy : MonoBehaviour
{
    FlowManager flow;
    BodyManager body;
    DanceManager dance;
    VideoCaptureCam video;
    LoginForm login;

    void Awake()
    {
        ResolveRefs();
    }

    void ResolveRefs()
    {
        if (flow == null)
            flow = FindFirstObjectByType<FlowManager>(FindObjectsInactive.Include);
        if (body == null)
            body = FindFirstObjectByType<BodyManager>(FindObjectsInactive.Include);
        if (dance == null)
            dance = FindFirstObjectByType<DanceManager>(FindObjectsInactive.Include);
        if (video == null)
            video = FindFirstObjectByType<VideoCaptureCam>(FindObjectsInactive.Include);
        if (login == null)
            login = FindFirstObjectByType<LoginForm>(FindObjectsInactive.Include);
    }

    public void setBackground(int bgNum)
    {
        ResolveRefs();
        if (flow != null)
            flow.setBackground(bgNum);
    }

    public void setBlackScreen(bool on)
    {
        ResolveRefs();
        if (flow != null)
            flow.setBlackScreen(on);
    }

    public void setFirstPage(int idx)
    {
        ResolveRefs();
        if (flow != null)
            flow.setFirstPage(idx);
    }

    public void ResetPage()
    {
        ResolveRefs();
        if (flow != null)
            flow.ResetPage();
    }

    public void SetWebCamActive(bool active)
    {
        ResolveRefs();
        if (flow != null && flow.WebCamObject != null)
            flow.WebCamObject.gameObject.SetActive(active);
    }

    public void EndMainGame()
    {
        ResolveRefs();
        if (body != null) {
            body.EndMainGame();
            return;
        }

        MBodyDiagLog.Warn("PackProxy", "EndMainGame: BodyManager not found");
    }

    public void set_danceNum(int value)
    {
        ResolveRefs();
        if (dance != null)
            dance.danceNum = value;
    }

    public void set_level(int value)
    {
        ResolveRefs();
        if (dance != null)
            dance.level = value;
    }

    public void set_DanceTiming(bool value)
    {
        ResolveRefs();
        if (dance != null)
            dance.DanceTiming = value;
    }

    public void setPrefix(string prefix)
    {
        ResolveRefs();
        if (video != null)
            video.setPrefix(prefix);
    }

    public void UpdateResult()
    {
        ResolveRefs();
        if (login != null)
            login.UpdateResult();
    }
}
