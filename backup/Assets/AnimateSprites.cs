using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnimateSprites : MonoBehaviour
{

    // Start is called before the first frame update
    void Awake() {
        img = GetComponent<Image>();
    }

    private Image img;
    public float duration;

    public Sprite[] sprites;
    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable() {
        StartCoroutine(ObjectOn());
    }

    IEnumerator ObjectOn() {
        for (int i = 0; i < sprites.Length; i++) {
            img.sprite = sprites[i];
            yield return new WaitForSecondsRealtime(duration);
        }
        GameObject.Destroy(this.gameObject);
    }
}
