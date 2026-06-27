using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class DancePreviewIcon : MonoBehaviour {
    public VideoClip[] SoftVideos;
    public VideoClip[] PowerVideos;
    public Sprite[] SoftIcons;
    public Sprite[] PowerIcons;
    public FlowManager fm;
    public DanceManager dm;
    public Image SoftTarget;
    public Image PowerTarget;
    public Image DancePlate;
    private float by = 1059f;
    public Text txt;
    // Start is called before the first frame update
    void Awake() {
        fm = FindFirstObjectByType<FlowManager>();
        dm = FindFirstObjectByType<DanceManager>();
    }

    private void OnEnable() {
        BodyResourceLifecycle.ReleaseBodyCapture();
        int dn = dm.danceNum;
        SoftTarget.sprite = SoftIcons[dn-1];
        PowerTarget.sprite = PowerIcons[dn - 1];
        SoftTarget.SetNativeSize();
        PowerTarget.SetNativeSize();
        if (SoftTarget.rectTransform.rect.height > by)
            SoftTarget.transform.localScale = new Vector3(by / SoftTarget.rectTransform.rect.height,
                by / SoftTarget.rectTransform.rect.height, by / SoftTarget.rectTransform.rect.height);
        if (PowerTarget.rectTransform.rect.height > by)
            PowerTarget.transform.localScale = new Vector3(by / PowerTarget.rectTransform.rect.height,
                by / PowerTarget.rectTransform.rect.height, by / PowerTarget.rectTransform.rect.height);
        txt.text = MBodyKoreanText.DancePreviewIntro(dn);
        DancePlate.gameObject.SetActive(false);
    }

    public VideoPlayer vp;

    public void playPreviewButton(bool isSoft) {
        DancePlate.gameObject.SetActive(true);
        vp.clip = isSoft ? SoftVideos[dm.danceNum - 1] : PowerVideos[dm.danceNum - 1];
        var ui = vp.GetComponent<UiVideoPlayer>();
        if (ui != null)
            ui.RestartPlayback();
        else
            vp.Play();
    }

    public void closeButton() {
        vp.Stop();
        DancePlate.gameObject.SetActive(false);
        
    }
}
