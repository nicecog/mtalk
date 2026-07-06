using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonFlow : MonoBehaviour
{
    public GameObject SelfPage;

    public GameObject NextPage;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void ClickNext()
    {
        if (isAudioStops) {
            AudioSource auso = GetComponent<AudioSource>();
            if (auso.isPlaying)
                return;
        }
        SelfPage.gameObject.SetActive(false);
        NextPage.gameObject.SetActive(true);
    }
    // Update is called once per frame
    void Update()
    {
        
    }

    public bool isAudioStops = false;
}
