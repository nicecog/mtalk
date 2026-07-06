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
        yield return new WaitForSecondsRealtime(WaitSeconds);
        SelfPage.gameObject.SetActive(false);
        NextPage.gameObject.SetActive(true);
    }
}
