using UnityEngine;

/// <summary>Login userdata 참조 — UI는 Bootstrap Canvas에 유지 (DDOL 분리 금지).</summary>
public class MSocialAppServices : MonoBehaviour
{
    public static MSocialAppServices Instance { get; private set; }

    public Login UserData { get; private set; }
    public FlowManager Flow { get; private set; }

    public static MSocialAppServices EnsureExists()
    {
        if (Instance != null)
            return Instance;
        var go = new GameObject(nameof(MSocialAppServices));
        Instance = go.AddComponent<MSocialAppServices>();
        DontDestroyOnLoad(go);
        return Instance;
    }

    public void BindLogin(Login login)
    {
        if (login != null)
            UserData = login;
    }

    public void BindFlow(FlowManager flow)
    {
        Flow = flow;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
