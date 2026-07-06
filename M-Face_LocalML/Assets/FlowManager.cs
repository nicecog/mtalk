using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;

public class FlowManager : MonoBehaviour
{
    
    public GameObject FirstPage;
    /// <summary>로그인 후 트레이닝 중 '홈' 버튼 목적지 (Stage1Start). FirstPage(IntroLogo)와 분리.</summary>
    public GameObject HomePage;

    void Awake() {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        StartCoroutine(CameraInitialize());
        Application.runInBackground = true;
        ResetPage();
        HomePop.SetActive(false);
        InfoPop.SetActive(false);
        QuitConfirm.SetActive(false);
    }

    IEnumerator CameraInitialize() {
        string[] pm = {
            Permission.Camera, Permission.ExternalStorageWrite, Permission.ExternalStorageRead
        };
        if(!Permission.HasUserAuthorizedPermission(Permission.Camera))
            Permission.RequestUserPermissions(pm);
        yield return null;

        while (!Permission.HasUserAuthorizedPermission(Permission.Camera)) {
            yield return null;
            if (Time.fixedTime > 10f)
                break;
        }

        yield return null;
        
        if (Permission.HasUserAuthorizedPermission(Permission.Camera)) {
            devices = WebCamTexture.devices;
            deviceCount = devices.Length;
            deviceNum = 1;
            for(int i=0;i< deviceCount;i++) {
                if (devices[i].isFrontFacing == true)
                    deviceNum = i;
            }
#if UNITY_EDITOR
            deviceNum = 0;
#endif
        }

        MFaceAndroidLog.Info("Warming up LocalML...");
        yield return MFaceLocalEmotionAnalyzer.EnsureInitialized();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ResetPage() {
        GoToPage(FirstPage);
    }

    void GoToPage(GameObject target) {
        if (target == null)
            return;

        GameObject[] gms = GameObject.FindGameObjectsWithTag("Page");
        foreach (var item in gms) {
            if (item.name != target.name)
                item.SetActive(false);
        }
        target.SetActive(true);
    }

    public void HomeButton() {
        //ResetPage();
        HomePop.SetActive(true);
    }

    public GameObject HomePop;

    public void HomeClose() {
        HomePop.SetActive(false);
    }

    public void HomeReset() {
        HomePop.SetActive(false);
        StopTrainingAudio();
        GoToPage(HomePage != null ? HomePage : FirstPage);
    }

    static void StopTrainingAudio() {
        var controllers = FindObjectsByType<AudioController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var ac in controllers) {
            if (ac.asc != null)
                ac.asc.Stop();
            ac.voiceOn = false;
        }
    }
    public void InfoButton() {
        InfoPop.SetActive(true);
    }

    public void InfoClose() {
        InfoPop.SetActive(false);
    }
    public GameObject InfoPop;

    public void CloseGame() {
        //Application.Quit();
        QuitConfirm.SetActive(true);
    }

    public GameObject QuitConfirm;

    public void QuitConfirmClose() {
       QuitConfirm.SetActive(false); 
    }

    public void QuitConfirmQuit() {
        Application.Quit();
    }
    public int deviceNum = 0;
    public int deviceCount = 0;
    public WebCamDevice[] devices;
}
