using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimerFlow : MonoBehaviour
{
    public GameObject SelfPage;
    
    public GameObject NextPage;

    public float WaitSeconds;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        StartCoroutine(WaitForNextScene());
    }

    private IEnumerator WaitForNextScene()
    {
        float wait = WaitSeconds;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (MSocialDeviceProfile.IsLowMemoryDevice)
            wait = Mathf.Min(WaitSeconds, 0.5f);
#endif
        yield return new WaitForSecondsRealtime(wait);
        if (NextPage != null)
        {
            var flow = MSocialAppServices.Instance != null ? MSocialAppServices.Instance.Flow : null;
            if (flow == null)
                flow = FindFirstObjectByType<FlowManager>();
            if (flow != null)
            {
                if (MSocialSceneRouter.TryResolvePage(NextPage, out var nextId) && nextId != MSocialPageId.None)
                    flow.GoToPage(nextId);
                else
                    flow.GoToPage(NextPage);
            }
            else
            {
                if (SelfPage != null)
                    SelfPage.SetActive(false);
                NextPage.SetActive(true);
            }
        }
        else if (SelfPage != null)
            SelfPage.SetActive(false);
    }
}
