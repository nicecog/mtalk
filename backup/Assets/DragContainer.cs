using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DragContainer : MonoBehaviour
{
    public Sprite alpha;

    public int indexData = 0;
    public DragIcon sourceIcon;
    // Start is called before the first frame update
    void Start()
    {
        gameObject.GetComponent<Image>().sprite = alpha;
        
    }

    public void resetSprite()
    {
        gameObject.transform.GetChild(0).GetComponent<Image>().sprite = alpha;
        gameObject.GetComponent<Image>().sprite = alpha;
    }
}
