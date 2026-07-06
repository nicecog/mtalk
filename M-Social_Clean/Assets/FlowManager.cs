using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;

[DefaultExecutionOrder(-400)]
public class FlowManager : MonoBehaviour
{
    public GameObject FirstPage;
    public GameObject HomePage;

    public MSocialPageId firstPageId = MSocialPageId.IntroLogo;
    public MSocialPageId homePageId = MSocialPageId.Stage1Start;

    void Awake() {
        MSocialStartupLog.Log("FlowManager.Awake");
        MSocialVideoGuard.DisarmAll();
        Application.runInBackground = true;
        MSocialAppServices.EnsureExists().BindFlow(this);
        if (FirstPage != null && MSocialPageMarker.FromLegacyName(FirstPage.name) != MSocialPageId.None)
            firstPageId = MSocialPageMarker.FromLegacyName(FirstPage.name);
        if (HomePage != null && MSocialPageMarker.FromLegacyName(HomePage.name) != MSocialPageId.None)
            homePageId = MSocialPageMarker.FromLegacyName(HomePage.name);
    }

    void Start() {
        if (InfoPop != null)
            InfoPop.SetActive(false);
        ResetPage();
        MSocialStartupLog.Log("FlowManager.Start -> " + firstPageId);
    }

    public void EnsureCameraReady() {
        if (cameraInitStarted)
            return;
        cameraInitStarted = true;
        StartCoroutine(CameraInitialize());
    }

    bool cameraInitStarted;

    IEnumerator CameraInitialize() {
        MSocialStartupLog.Log("CameraInitialize start");
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            Permission.RequestUserPermission(Permission.Camera);

        float deadline = Time.realtimeSinceStartup + 10f;
        while (!Permission.HasUserAuthorizedPermission(Permission.Camera)) {
            if (Time.realtimeSinceStartup > deadline)
                break;
            yield return null;
        }

        if (Permission.HasUserAuthorizedPermission(Permission.Camera)) {
            devices = WebCamTexture.devices;
            deviceCount = devices.Length;
            deviceNum = 1;
            for (int i = 0; i < deviceCount; i++) {
                if (devices[i].isFrontFacing)
                    deviceNum = i;
            }
#if UNITY_EDITOR
            deviceNum = 0;
#endif
            MSocialStartupLog.Log("Camera ready devices=" + deviceCount);
        }
        else {
            MSocialStartupLog.Log("Camera permission denied or timeout");
        }
    }

    public void ResetPage() {
        if (FirstPage != null && FirstPage.scene.name == MSocialSceneRouter.BootstrapScene)
            ShowBootstrapPage(FirstPage);
        else
            GoToPage(firstPageId);
    }

    void ShowBootstrapPage(GameObject target) {
        MSocialPageRegistry.EnsureRegistered();
        var targetId = MSocialPageMarker.FromLegacyName(target.name);
        foreach (var marker in FindObjectsByType<MSocialPageMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
            if (marker.pageId == MSocialPageId.Background)
                continue;
            marker.gameObject.SetActive(marker.gameObject == target);
        }
        MSocialPageRegistry.Get(MSocialPageId.Background)?.SetActive(true);
        target.SetActive(true);
        MSocialStartupLog.Log("Bootstrap show page " + target.name);
    }

    public void GoToPage(MSocialPageId targetId) {
        if (targetId == MSocialPageId.None)
            return;
        StartCoroutine(MSocialSceneRouter.ShowPage(targetId, this));
    }

    public void GoToPage(GameObject target) {
        if (target == null)
            return;
        if (target.scene.name == MSocialSceneRouter.BootstrapScene) {
            ShowBootstrapPage(target);
            return;
        }
        var id = MSocialPageMarker.FromLegacyName(target.name);
        if (id != MSocialPageId.None)
            GoToPage(id);
        else
            LegacyGoToPage(target);
    }

    void LegacyGoToPage(GameObject target) {
        GameObject[] gms = GameObject.FindGameObjectsWithTag("Page");
        foreach (var item in gms) {
            if (item.name != target.name)
                item.SetActive(false);
        }
        target.SetActive(true);
    }

    public void HomeButton() {
        HomeReset();
    }

    public void HomeReset() {
        StopTrainingAudio();
        EnsureCameraReady();
        GoToPage(homePageId);
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
        if (InfoPop != null)
            InfoPop.SetActive(true);
    }

    public void InfoClose() {
        if (InfoPop != null)
            InfoPop.SetActive(false);
    }
    public GameObject InfoPop;

    public void CloseGame() {
        Application.Quit();
    }

    public int deviceNum = 0;
    public int deviceCount = 0;
    public WebCamDevice[] devices;
}
