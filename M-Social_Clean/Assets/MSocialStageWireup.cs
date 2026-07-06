using UnityEngine;

/// <summary>씬 분리 후 끊긴 Login·Stage2Intro 등 크로스 씬 참조 복구.</summary>
public static class MSocialStageWireup
{
    public static void WireAll()
    {
        MSocialPageRegistry.Refresh();

        var login = MSocialAppServices.Instance != null ? MSocialAppServices.Instance.UserData : null;
        if (login == null)
            login = Object.FindFirstObjectByType<Login>(FindObjectsInactive.Include);

        foreach (var sm in Object.FindObjectsByType<Stage1Manager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sm.userdata == null && login != null)
                sm.userdata = login;
            if (sm.Stage2Intro == null)
                sm.Stage2Intro = MSocialPageRegistry.Get(MSocialPageId.Stage2Intro);
        }

        foreach (var es in Object.FindObjectsByType<EmotionSelect>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (es.userdata == null && login != null)
                es.userdata = login;
            if (es.scp == null)
            {
                var sp = Object.FindFirstObjectByType<ScenePlayer>(FindObjectsInactive.Include);
                if (sp != null)
                    es.scp = sp;
            }
        }

        foreach (var neg in Object.FindObjectsByType<Stage1Negative>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (neg.userdata == null && login != null)
                neg.userdata = login;
        }

        if (login != null)
            MSocialStartupLog.Log("StageWireup login=" + login.ID);
    }
}
