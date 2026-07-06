using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class InstructionChange2 : MonoBehaviour
{
    public Image img;
    public Sprite[] mainSprites;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void updateImage(int idx) {
        img.sprite = mainSprites[idx];
        img.SetNativeSize();
        
    }
}
