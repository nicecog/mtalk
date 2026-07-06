using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class screenDuplicate : MonoBehaviour {

    public Image img;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnEnable() {
        GetComponent<Image>().sprite = img.sprite;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
