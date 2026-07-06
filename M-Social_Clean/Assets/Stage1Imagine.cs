using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Stage1Imagine : MonoBehaviour
{

    private AudioSource asc;

    public AudioClip[] sources;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnEnable() {
        asc = GetComponent<AudioSource>();
        asc.loop = false;
        asc.playOnAwake = false;


        nextButton.gameObject.SetActive(false);
        StartCoroutine(WaitForVideoEnd());
    }


    IEnumerator WaitForVideoEnd() {
        asc.clip = sources[0];
        asc.Play();
        yield return new WaitForSeconds(1f);
        while (asc.isPlaying)
            yield return null;
        yield return new WaitForSeconds(1f);

        asc.clip = sources[1];
        asc.Play();
        yield return new WaitForSeconds(1f);
        while (asc.isPlaying)
            yield return null;
        yield return new WaitForSeconds(1f);
        //do something
        nextButton.gameObject.SetActive(true);
    }

    public Button nextButton;
    // Update is called once per frame
    void Update()
    {
        
    }

    public void ClickNextButton() {
        gameObject.SetActive(false);
        s1m.ProgressDone();
    }

    public Stage1Manager s1m;
}
