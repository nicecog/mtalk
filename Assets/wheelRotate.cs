using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class wheelRotate : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake() {
        self = GetComponent<Image>();
        wheel = transform.GetChild(0).GetComponent<Image>();
    }

    private Image wheel;

    private Image self;
    // Update is called once per frame
    void Update() {
        wheel.enabled = self.enabled;
        if(wheel.enabled)
            wheel.transform.Rotate(Vector3.forward * (180 * Time.deltaTime));
    }

}
