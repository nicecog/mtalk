using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogoutPopup : MonoBehaviour
{
    private void OnEnable() {
        homeButton.SetActive(false);
        PopUpObject.SetActive(false);
    }

    private void OnDisable() {
        homeButton.SetActive(true);
    }

    public GameObject homeButton;
    public GameObject PopUpObject;
    public void pop() {
        PopUpObject.SetActive(true);
    }

    public void exit() {
        PopUpObject.SetActive(false);
    }
}
