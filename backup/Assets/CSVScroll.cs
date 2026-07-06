using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class CSVScroll : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake() {
        dm = FindFirstObjectByType<DanceManager>();
    }

    private int max = 100;
    
    private void OnEnable() {
        max = 100;
        StreamReader sr = new StreamReader(MBodyPaths.DataRoot + "/saveData_"+dm.ID+".csv");
        string s = sr.ReadLine();
        Debug.Log(s);
        List<string> lines = new List<string>();
        while (true) {
            string dataString = sr.ReadLine();
            if (dataString == null) {
                break;
            }
            lines.Add(dataString);
        }

        int body = 0;
        bool pass = false;
        for (int i = lines.Count - 1; i >= 0; i--) {
            Debug.Log(lines[i]);
            if (!lines[i].Contains("Body"))
                continue;
            max--;
            if (max <= 0)
                pass = true;
            if (!pass) {
                GameObject temp = GameObject.Instantiate(childObject, scrollHead.transform);
                temp.transform.GetChild(0).GetComponent<Text>().text = (100 - max).ToString();
                string date = lines[i].Split(',')[0];
                date = date.Split(' ')[0];
                string[] ymd = date.Split('-');

                temp.transform.GetChild(1).GetComponent<Text>().text =
                    string.Concat(ymd[0], ymd[1], ymd[2]);
                temp.transform.GetChild(2).GetComponent<Text>().text = lines[i].Split(',')[3];
            }

            body++;
        }

        CountText.text = body.ToString();
    }

    public Text CountText;
    public GameObject scrollHead;
    public GameObject childObject;
    public DanceManager dm;
}
