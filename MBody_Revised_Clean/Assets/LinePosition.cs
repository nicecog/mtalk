using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LinePosition : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake() {
        img = GetComponent<Image>();
        OriginalVec = img.rectTransform.anchoredPosition;
    }

    private Image img;
    // Update is ca    lled once per frame
    void Update()
    {
        
    }

    private void OnEnable() {
        Vector2 v = nm.dsm.GameLevel >= 2 ? UpVec : Vector2.zero;
        GetComponent<Image>().rectTransform.anchoredPosition = OriginalVec + v;
    }

    private Vector2 OriginalVec;
    public Vector2 UpVec;
    public NoteManager nm;
}
