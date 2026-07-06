using UnityEngine;
using UnityEngine.UI;

/// <summary>Bootstrap 씬 — Router·AppServices·저사양 Canvas 초기화.</summary>
[DefaultExecutionOrder(-500)]
public class MSocialSceneBootstrap : MonoBehaviour
{
    public RectTransform pageContainer;
    public FlowManager flowManager;
    public Login login;

    void Awake()
    {
        if (pageContainer == null)
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
                pageContainer = canvas.GetComponent<RectTransform>();
        }

        var services = MSocialAppServices.EnsureExists();
        if (login == null)
            login = FindFirstObjectByType<Login>(FindObjectsInactive.Include);
        if (flowManager == null)
            flowManager = FindFirstObjectByType<FlowManager>(FindObjectsInactive.Include);
        if (login != null)
            services.BindLogin(login);
        if (flowManager != null)
            services.BindFlow(flowManager);

        MSocialSceneRouter.Initialize(pageContainer != null ? pageContainer : transform);
        MSocialDeviceProfile.ApplyBootstrapCanvas(pageContainer);
        MSocialStartupLog.Log("SceneBootstrap ready");
    }
}
