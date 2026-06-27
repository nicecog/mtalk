using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;
using UnityEngine.Video;

public class FlowManager : MonoBehaviour {
    static readonly HashSet<string> IntroLoginPages = new HashSet<string>
    {
        "IntroLogo",
        "MBodyLogin"
    };

    static readonly HashSet<string> BodyHandPages = new HashSet<string>
    {
        "MainGameCali",
    };
    public int level;
    public GameObject FirstPage;

    public RawImage WebCamObject;

    public RawImage smallPlate;
    void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            Permission.RequestUserPermission(Permission.Camera);
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);
#endif
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.runInBackground = true;
        OriginalSize = WebCamObject.rectTransform.sizeDelta;
        OriginalPosition = WebCamObject.rectTransform.anchoredPosition;
        OriginalPage = FirstPage;
        ResolveFlowPages();
        ResolveTrainingRoots();
        ResetPage();
        SceneFlowRegistry.Refresh();
        SceneFlowVideoInstaller.InstallAll();
        if (GetComponent<ScenePageVideoDriver>() == null)
            gameObject.AddComponent<ScenePageVideoDriver>();
        MBodyDiagLog.Step("Flow", $"Awake complete. diagLog={MBodyDiagLog.LogFilePath}");
        var optimizer = gameObject.GetComponent<MBodyStartupOptimizer>();
        if (optimizer == null)
            optimizer = gameObject.AddComponent<MBodyStartupOptimizer>();
    }

    public GameObject OriginalPage;
    public void ResetPage()
    {
        smallPlate.enabled = false;
        GameObject[] scenes = GameObject.FindGameObjectsWithTag("SceneFlow");
        for (int i = 0; i < scenes.Length; i++)
        {
            scenes[i].gameObject.SetActive(false);
        }
        setBackground(0);
        setBlackScreen(false);
        SetTrainingRootsActive(false);
        StopAllSceneVideos();
        FirstPage.gameObject.SetActive(true);
        OnSceneFlowPageShown(FirstPage);
        WebCamObject.gameObject.SetActive(false);
        var bm = FindFirstObjectByType<BodyManager>(FindObjectsInactive.Include);
        var dm = FindFirstObjectByType<DanceManager>(FindObjectsInactive.Include);
        if (bm != null)
            bm.ResetBodyManager();
        else
            MBodyDiagLog.Warn("Flow", "BodyManager not found during ResetPage");
        if (dm != null)
            dm.ResetDanceManager();
        else
            MBodyDiagLog.Warn("Flow", "DanceManager not found during ResetPage");
        OriginalScale();
    }

    private Vector2 OriginalSize;
    private Vector2 OriginalPosition;
    public void OriginalScale() {
        WebCamObject.rectTransform.sizeDelta = OriginalSize;
        WebCamObject.rectTransform.anchoredPosition = OriginalPosition;
    }

    public void SmallScale(Vector2 size, Vector2 pos) {
        WebCamObject.rectTransform.sizeDelta = size;
        WebCamObject.rectTransform.anchoredPosition = pos;
    }

    void Update()
    {
        if (LeftHand == null || RightHand == null)
            return;

        if (!WebCamObject.IsActive()) {
            LeftHand.gameObject.SetActive(false);
            RightHand.gameObject.SetActive(false);
        }
        else {
            LeftHand.gameObject.SetActive(true);
            RightHand.gameObject.SetActive(true);
        }
    }

    public bool isDebug = false;
    public Image LeftHand;
    public Image RightHand;
    public void UpdateAI(Vector2 left_coord, Vector2 right_coord, float conf) {
        
        if(isDebug)
            Debug.Log("Left : ["+left_coord.x+", "+left_coord.y+"], Right : ["+right_coord.x+", "+right_coord.y+"], Confidence : "+conf);
        LeftHand.rectTransform.anchoredPosition = left_coord;
        RightHand.rectTransform.anchoredPosition = right_coord;
        
    }
    
    
    public Image bg;
    public GameObject alpha;
    public Sprite[] bg_sprites;
    
    public void setBackground(int bgNum){
        if (bgNum < bg_sprites.Length)
            bg.sprite = bg_sprites[bgNum];

    }

    public void shutDown() {
        Application.Quit();
    }
    public void setBlackScreen(bool alphaOn) {
        if (alphaOn)
            alpha.SetActive(true);
        else
            alpha.SetActive(false);
        
    }

    public void setFirstPage(int idx) {
        FirstPage = idx == 0 ? OriginalPage : idx == 1 ? BodyPage : DancePage;
    }

    public GameObject BodyPage, DancePage;

    GameObject bodyTrainingRoot;
    GameObject danceTrainingRoot;

    public void OnSceneFlowPageShown(string pageName)
    {
        OnSceneFlowPageShown(SceneFlowRegistry.Get(pageName));
    }

    public void OnSceneFlowPageShown(GameObject page)
    {
        if (page == null)
        {
            MBodyDiagLog.Warn("Flow", "OnSceneFlowPageShown called with null page");
            return;
        }

        var pageName = page.name;
        MBodyDiagLog.Step("Flow", $"Page shown name={pageName} id={page.GetInstanceID()} parent={DescribeTransform(page.transform)}");

        if (IntroLoginPages.Contains(pageName))
        {
            SetTrainingRootsActive(false);
            SetFrontLayerActive(false);
            setBlackScreen(false);
            StopAllSceneVideos();
            BringPageToFront(page);
            return;
        }

        if (pageName == "IntroVideo")
        {
            ConfigureTrainingRootsForIntro(page);
            SetFrontLayerActive(false);
            setBlackScreen(false);
            BringPageToFront(page);
            LogIntroVideoPlayers(page);
            StartPageVideos(page);
            return;
        }

        SetFrontLayerActive(true);
        setBlackScreen(false);
        SetTrainingRootsActive(ShouldShowTrainingRoots(pageName));
        BringPageToFront(page);
        bool handPage = BodyHandPages.Contains(pageName);
        BodyResourceLifecycle.SetPoseProcessing(handPage);
        if (handPage)
            BringHandMarkersToFront();
        else
            RestoreHandMarkers();
        StartPageVideos(page);
    }

    void StartPageVideos(GameObject page)
    {
        if (page == null)
            return;

        if (page.GetComponentsInChildren<VideoPlayer>(true).Length == 0)
            return;

        if (ScenePageVideoDriver.Instance != null)
            ScenePageVideoDriver.Instance.PlayOnPage(page);
        else
            MBodyDiagLog.Warn("Flow", "ScenePageVideoDriver missing; videos may not start");
    }

    void ConfigureTrainingRootsForIntro(GameObject introPage)
    {
        ResolveTrainingRoots();
        var underBody = bodyTrainingRoot != null && introPage.transform.IsChildOf(bodyTrainingRoot.transform);
        var underDance = danceTrainingRoot != null && introPage.transform.IsChildOf(danceTrainingRoot.transform);

        if (bodyTrainingRoot != null)
            bodyTrainingRoot.SetActive(underBody);
        if (danceTrainingRoot != null)
            danceTrainingRoot.SetActive(underDance);

        MBodyDiagLog.Step("Flow", $"IntroVideo training roots body={underBody} dance={underDance}");
    }

    static void LogIntroVideoPlayers(GameObject introPage)
    {
        var players = introPage.GetComponentsInChildren<VideoPlayer>(true);
        for (var i = 0; i < players.Length; i++)
        {
            var vp = players[i];
            if (vp == null)
                continue;

            var clip = vp.clip;
            var clipInfo = clip != null
                ? $"clip='{clip.name}' {clip.width}x{clip.height}"
                : "clip=null";
            MBodyDiagLog.Step("Flow", $"IntroVideo VP[{i}] go='{vp.gameObject.name}' {clipInfo} renderMode={vp.renderMode}");
        }
    }

    static string DescribeTransform(Transform t)
    {
        if (t == null)
            return "(null)";
        return t.parent != null ? $"{t.parent.name}/{t.name}" : t.name;
    }

    void SetFrontLayerActive(bool active)
    {
        var front = transform.Find("Front");
        if (front != null)
            front.gameObject.SetActive(active);
    }

    void BringPageToFront(GameObject page)
    {
        if (page == null)
            return;

        page.transform.SetAsLastSibling();
    }

    public void BringHandMarkersToFront()
    {
        if (LeftHand == null || RightHand == null)
            return;

        if (WebCamObject != null && WebCamObject.IsActive()) {
            LeftHand.gameObject.SetActive(true);
            RightHand.gameObject.SetActive(true);
        }
    }

    public void RestoreHandMarkers()
    {
    }

    static bool ShouldShowTrainingRoots(string pageName)
    {
        if (IntroLoginPages.Contains(pageName) || pageName == "IntroVideo" || pageName == "BodyDanceSelect")
            return false;

        return true;
    }

    public void StopAllSceneVideos()
    {
        var players = FindObjectsByType<VideoPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (var i = 0; i < players.Length; i++)
        {
            var vp = players[i];
            if (vp == null)
                continue;

            if (vp.isPlaying)
                vp.Stop();
        }
    }

    public void SetTrainingRootsActive(bool active)
    {
        ResolveTrainingRoots();
        if (bodyTrainingRoot != null)
            bodyTrainingRoot.SetActive(active);
        if (danceTrainingRoot != null)
            danceTrainingRoot.SetActive(active);
        MBodyDiagLog.Step("Flow", $"SetTrainingRootsActive({active}) body={(bodyTrainingRoot != null)} dance={(danceTrainingRoot != null)}");
    }

    void ResolveTrainingRoots()
    {
        if (bodyTrainingRoot != null && danceTrainingRoot != null)
            return;

        var scenesRoot = transform;
        if (bodyTrainingRoot == null)
        {
            var body = scenesRoot.Find("BodyTraining");
            if (body != null)
                bodyTrainingRoot = body.gameObject;
        }

        if (danceTrainingRoot == null)
        {
            var dance = scenesRoot.Find("DanceTraining");
            if (dance != null)
                danceTrainingRoot = dance.gameObject;
        }
    }

    void ResolveFlowPages()
    {
        if (BodyPage == null)
            BodyPage = SceneFlowRegistry.Get("MusicSelect");
        if (DancePage == null)
            DancePage = SceneFlowRegistry.Get("DanceSelect");
    }
}
