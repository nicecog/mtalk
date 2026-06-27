using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class WebCamInput : MonoBehaviour
{
    [SerializeField] string webCamName;
    Vector2 webCamResolution = new Vector2(1920, 1080);
    [SerializeField] Texture staticInput;

    public Texture inputImageTexture {
        get {
            if (staticInput != null)
                return staticInput;
            return inputRT;
        }
    }

    public WebCamTexture webCamTexture;
    RenderTexture inputRT;

    bool initStarted;

    void Start()
    {
        StartCoroutine(InitCaptureCoroutine());
    }

    /// <summary>
    /// Re-open the camera after ReleaseCapture (Start only runs once).
    /// </summary>
    public void EnsureCapture()
    {
        if (staticInput != null)
            return;

        if (webCamTexture != null && webCamTexture.isPlaying && inputRT != null)
            return;

        if (!initStarted)
            StartCoroutine(InitCaptureCoroutine());
    }

    IEnumerator InitCaptureCoroutine()
    {
        initStarted = true;

        var performance = PerformanceManager.EnsureExists();
        if (!performance.IsReady)
            yield return performance.InitializeAsync();

        webCamResolution = new Vector2(
            performance.CameraResolution.x,
            performance.CameraResolution.y);

        if (inputRT == null) {
            inputRT = new RenderTexture((int)webCamResolution.x, (int)webCamResolution.y, 0);
        }

        if (webCamTexture == null || !webCamTexture.isPlaying) {
            if (webCamTexture != null) {
                if (webCamTexture.isPlaying)
                    webCamTexture.Stop();
                Destroy(webCamTexture);
                webCamTexture = null;
            }

            WebCamDevice[] dvs = WebCamTexture.devices;
            if (dvs.Length > 3)
                webCamName = WebCamTexture.devices.Last().name;
            else if (dvs.Length > 0)
                webCamName = WebCamTexture.devices.First().name;

            if (!string.IsNullOrEmpty(webCamName)) {
                webCamTexture = new WebCamTexture(webCamName, (int)webCamResolution.x, (int)webCamResolution.y);
                webCamTexture.Play();
            }
        }

        if (text != null) {
            if (isDebug && webCamTexture != null)
                text.text = "Width : " + webCamTexture.width + ", Height : " + webCamTexture.height;
            else
                text.text = "";
        }

        initStarted = false;
    }

    public Text text;
    public bool isDebug = false;

    void Update()
    {
        if (staticInput != null || webCamTexture == null || inputRT == null)
            return;
        if (!webCamTexture.didUpdateThisFrame)
            return;

        var aspect1 = (float)webCamTexture.width / webCamTexture.height;
        var aspect2 = (float)inputRT.width / inputRT.height;
        var aspectGap = aspect2 / aspect1;

        var vMirrored = webCamTexture.videoVerticallyMirrored;
        var scale = new Vector2(aspectGap, vMirrored ? -1 : 1);
        var offset = new Vector2((1 - aspectGap) / 2, vMirrored ? 1 : 0);

        Graphics.Blit(webCamTexture, inputRT, scale, offset);
    }

    public void ReleaseCapture()
    {
        if (webCamTexture != null) {
            if (webCamTexture.isPlaying)
                webCamTexture.Stop();
            Destroy(webCamTexture);
            webCamTexture = null;
        }

        if (inputRT != null) {
            inputRT.Release();
            Destroy(inputRT);
            inputRT = null;
        }
    }

    public void deviceChange()
    {
        if (staticInput != null)
            return;

        var devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0) {
            Debug.LogWarning("WebCamInput.deviceChange: no camera devices.");
            return;
        }

        if (devices.Length == 1) {
            Debug.Log("WebCamInput.deviceChange: only one camera available.");
            return;
        }

        var nextIndex = 0;
        if (!string.IsNullOrEmpty(webCamName)) {
            for (var i = 0; i < devices.Length; i++) {
                if (devices[i].name == webCamName) {
                    nextIndex = (i + 1) % devices.Length;
                    break;
                }
            }
        }

        StartCoroutine(SwitchCameraCoroutine(devices[nextIndex].name));
    }

    IEnumerator SwitchCameraCoroutine(string nextDeviceName)
    {
        if (webCamTexture != null) {
            if (webCamTexture.isPlaying)
                webCamTexture.Stop();
            Destroy(webCamTexture);
            webCamTexture = null;
        }

        webCamName = nextDeviceName;
        webCamTexture = new WebCamTexture(webCamName, (int)webCamResolution.x, (int)webCamResolution.y);
        webCamTexture.Play();

        while (webCamTexture.width <= 16)
            yield return null;

        if (text != null && isDebug)
            text.text = "Width : " + webCamTexture.width + ", Height : " + webCamTexture.height;

        Debug.Log($"WebCamInput.deviceChange: switched to '{webCamName}'");
    }

    void OnDestroy()
    {
        if (webCamTexture != null)
            Destroy(webCamTexture);
        if (inputRT != null)
            Destroy(inputRT);
    }
}
