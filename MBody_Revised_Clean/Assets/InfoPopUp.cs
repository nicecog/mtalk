using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InfoPopUp : MonoBehaviour
{
    public Image popPanel;
    public GameObject[] beforeSelect;
    public void popUp() {
        bool NonSelected = false;
        foreach(var v in beforeSelect) {
            if (v.activeSelf)
                NonSelected = true;
        }
        if(!NonSelected)
            popPanel.gameObject.SetActive(true);
    }

    public void backButton() {
        popPanel.gameObject.SetActive(false);
    }

    private void Awake() {
        popPanel.gameObject.SetActive(false);
    }
}
