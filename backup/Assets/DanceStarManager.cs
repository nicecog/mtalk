using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DanceStarManager : MonoBehaviour
{
    // Start is called before the first frame update

    public Sprite[] T1, T2, T3;
    public NoteManager nm;

    public int GameLevel = 1;
    void OnEnable() {
    }

    // Update is called once per frame
    void Update()
    {
        if (GameLevel >= 3 && nm._Play == false) {
            ReturnButton.gameObject.SetActive(true);
        }
        else {
            ReturnButton.gameObject.SetActive(false);
        }
    }

    public void setLevel(int t) {
        GameLevel = t;
        nm.noteTypes = GameLevel == 1 ? T1 : GameLevel == 2 ? T2 : T3;
    }

    public Button ReturnButton;
    public void NextLevel() {
        if (GameLevel == 1)
            setLevel(GameLevel + 1);
        else if (GameLevel==2) {
            setLevel(GameLevel + 1);
        }

    }

    public void RestartLevel() {
        setLevel(GameLevel);
        
    }

}
