using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProgressDots : MonoBehaviour
{
    public GameObject[] trials;

    public void setNumber(int num) {
        if (trials == null || trials.Length < 3)
            return;
        if (num > 3 || num < 1)
            return;
        if (num == 1) {
            trials[0].SetActive(true);
            trials[1].SetActive(false);
            trials[2].SetActive(false);
        } else if (num == 2) {
            trials[0].SetActive(true);
            trials[1].SetActive(true);
            trials[2].SetActive(false);
        }
        else if (num == 3) {
            trials[0].SetActive(true);
            trials[1].SetActive(true);
            trials[2].SetActive(true);
        }
    }
}
