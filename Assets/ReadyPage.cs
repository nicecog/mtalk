using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReadyPage : MonoBehaviour {
    public GameObject p1, p2, p3;

    private void OnEnable() {
        p2.SetActive(false);
        p3.SetActive(false);
        StartCoroutine(readyCoroutine());
    }

    private IEnumerator readyCoroutine() {
        p1.SetActive(true);
        yield return new WaitForSecondsRealtime(2f);
        p1.SetActive(false);
        p2.SetActive(true);
        yield return new WaitForSecondsRealtime(2f);
        p2.SetActive(false);
        p3.SetActive(true);
    }

    private void OnDisable() {
        StopAllCoroutines();
    }
}
