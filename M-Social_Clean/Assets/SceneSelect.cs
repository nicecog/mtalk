using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SceneSelect : MonoBehaviour {
    public Stage1Manager s1m;
    public int SceneLeft;
    public int sceneCategory;
    public Sprite alpha;
    public Button[] pageButtons;
    public int nowPage;
    private List<Sprite> t;
    private List<String> titles;
    private List<int> isPrv;
    private void Awake() {
        t = new List<Sprite>();
        titles=new List<String>();
        isPrv = new List<int>();
    }

    public Stage2Manager sm2;
    // Start is called before the first frame update
    private void OnEnable() {
        Stage1MainScene.SetActive(false);
        progress.text = s1m.userdata.playNum.ToString("00");
        sceneNumber = 0;
        nowPage = 0;
        maxPage = ((SceneLeft-1) / 2);
        mask[0].gameObject.SetActive(false);
        mask[1].gameObject.SetActive(SceneLeft<2);
        ConfirmButton.interactable = false;
        SceneButton[1].GetComponent<Button>().interactable = SceneLeft >1;
        SceneButton[0].color = Color.gray;
        SceneButton[1].color = Color.gray;
        t.Clear();
        titles.Clear();
        isPrv.Clear();
        ScenePlayer sp = Stage1MainScene.GetComponent<ScenePlayer>();
        var catalog = MSocialStageCatalog.Instance;
        for (int i = 0; i < 32; i++) {
            if (sp.VideoCodes[i] / 100 == sceneCategory) {
                t.Add(GetFirstFrame(catalog, i));
                titles.Add(sp.titles[i]);
                isPrv.Add(sp.prv[i]);
            }
        }

        if (t.Count > 0) {
            SceneButton[0].enabled = true;
            SceneButton[0].sprite = t[0];
            SceneButton[0].GetComponentInChildren<Text>().text = titles[0];
            SceneButton[0].SetNativeSize();
            SceneButton[0].rectTransform.sizeDelta *= 1132.0f / 1920.0f;
            int prv = s1m.progressCount < 5 ? 1 : 2;
            mask[0].gameObject.SetActive(prv!=isPrv[0]);
        }
        else {
            SceneButton[0].enabled = false;
            SceneButton[0].GetComponentInChildren<Text>().text = "";
        }

        if (t.Count > 1) {
            SceneButton[1].enabled = true;
            SceneButton[1].GetComponentInChildren<Text>().text = titles[1];
            SceneButton[1].sprite = t[1];
            SceneButton[1].SetNativeSize();
            SceneButton[1].rectTransform.sizeDelta *= 1132.0f / 1920.0f;
            int prv = s1m.progressCount < 5 ? 1 : 2;
            mask[1].gameObject.SetActive(prv!=isPrv[1]);
        }
        else {
            SceneButton[1].enabled = false;
            SceneButton[1].GetComponentInChildren<Text>().text = "";
        }

        pageButtons[0].image.enabled = true;
        pageButtons[1].image.enabled = true;
        pageButtons[0].image.enabled = false;
        if (maxPage == 0) pageButtons[1].image.enabled = false; 
        for (int i = 0; i < 6; i++) {
            TopCategory[i].sprite = TopIcons[i+(i==sceneCategory-1?6:0)];
        }
    }

    public Sprite[] TopIcons;

    public Image[] TopCategory;
    // Update is called once per frame
    void Update()
    {
        
    }

    public void Arrow(int left) {
        if (left == 1)
            nowPage--;
        else
            nowPage++;
        nowPage = nowPage < 0 ? 0 : nowPage;
        nowPage = nowPage > maxPage ? maxPage : nowPage;
        pageButtons[0].image.enabled = nowPage>0;
        pageButtons[1].image.enabled = nowPage<maxPage;
        int temp = SceneLeft - 2 * nowPage;

        mask[1].gameObject.SetActive(temp==1);
        SceneButton[1].GetComponent<Button>().interactable = (temp != 1);

        SceneButton[0].color = Color.gray;
        SceneButton[1].color = Color.gray;
        sceneNumber = 0;
        ConfirmButton.interactable = false;
        if (temp > 0) {
            SceneButton[0].sprite = t[nowPage * 2];
            SceneButton[0].GetComponentInChildren<Text>().text = titles[nowPage*2];
            int prv = s1m.progressCount < 5 ? 1 : 2;
            mask[0].gameObject.SetActive(prv!=isPrv[nowPage*2]);
        }
        else {
            
            SceneButton[0].GetComponentInChildren<Text>().text = "";
        }

        if (temp > 1) {
            SceneButton[1].sprite = t[(nowPage * 2) + 1];
            SceneButton[1].GetComponentInChildren<Text>().text = titles[nowPage*2 + 1];
            int prv = s1m.progressCount < 5 ? 1 : 2;
            mask[1].gameObject.SetActive(prv!=isPrv[nowPage*2+ 1]);
        }else
            SceneButton[1].GetComponentInChildren<Text>().text = "";
        
    }

    public Text progress;
    public int sceneNumber;
    public int maxPage;
    public void clickScene(int num) {
        if (num != sceneNumber) {
            sceneNumber = num;
            SceneButton[num-1].color = Color.white;
            SceneButton[2 - num].color = Color.gray;
            ConfirmButton.interactable = true;
            int temp = UnityEngine.Random.Range(0, 3);
            con.playNumber(temp);
        }
        else {
            SceneButton[0].color = Color.gray;
            SceneButton[1].color = Color.gray;
            sceneNumber = 0;
            ConfirmButton.interactable = false;
        }
        //sceneNumber = sceneCategory * 10 + num;
    }

    public Image[] SceneButton;
    public Image[] mask;

    public Sprite[] Category;
    public Button ConfirmButton;

    public void clickNextButton() {
        ConfirmButton.interactable = false;
        StartCoroutine(waitForNextScene());
    }

    IEnumerator waitForNextScene() {
        yield return new WaitForSeconds(1f);
        while (con.voiceOn)
            yield return null;
        yield return new WaitForSeconds(0.5f);
        Stage1MainScene.SetActive(true);
    }

    public GameObject Stage1MainScene;
    public AudioController con;

    static Sprite GetFirstFrame(MSocialStageCatalog catalog, int index) {
        if (catalog != null && catalog.firstFrames != null && index >= 0 && index < catalog.firstFrames.Length)
            return catalog.firstFrames[index];
        return null;
    }
}
