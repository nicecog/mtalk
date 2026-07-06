using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class InfoWindow : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public ItemSelect itemSel;
    public Login userdata;
    private Image window;
    private void OnEnable() {
        if (userdata != null && userdata.ID != "admin")
            StartCoroutine(SyncProgressThenShow());
        else
            ShowItems();
    }

    IEnumerator SyncProgressThenShow() {
        if (userdata != null)
            yield return userdata.SyncProgressFromServerCoroutine();
        ShowItems();
    }

    void ShowItems() {
        window = GetComponent<Image>();
        window.sprite = windowSprites[0];
        for (int i = 0; i < itemSel.ItemList.Length; i++) {
            boxs[i].sprite = userdata.ItemString[i]=='T'?itemSel.ItemList[i]:alpha;
            boxs[i].SetNativeSize();
            Rect xy = boxs[i].rectTransform.rect;
            float r = xy.width > xy.height ? xy.width : xy.height;
            r = (boxSize * 0.9f) / r;
            boxs[i].rectTransform.sizeDelta *= r;
        }
    }

    public void WindowChange(int n) {
        window.sprite= windowSprites[n];
    }

    public Sprite[] windowSprites;
    public Image[] boxs;
    public float boxSize;
    public Sprite alpha;
}
