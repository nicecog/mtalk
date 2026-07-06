using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyManager : MonoBehaviour {
    private FlowManager fm;

    public ProgressDots pds;

    public int playTimes = 0;
    // Start is called before the first frame update
    void Awake() {
        fm = FindFirstObjectByType<FlowManager>();
        ResolvePackRefs();
    }

    public void ResolvePackRefs()
    {
        if (yellowSelect == null)
            yellowSelect = FindSoundSelect("SoundSelectYellow");
        if (blueSelect == null)
            blueSelect = FindSoundSelect("SoundSelectBlue");
        if (musicSelect == null)
            musicSelect = SceneFlowRegistry.Get("MusicSelect");
        if (EndPage == null)
            EndPage = SceneFlowRegistry.Get("EndMessage");
        if (endSelect == null)
            endSelect = SceneFlowRegistry.Get("EndSelect (1)");
        if (pds == null)
            pds = FindFirstObjectByType<ProgressDots>(FindObjectsInactive.Include);
    }

    static SoundSelect FindSoundSelect(string pageName)
    {
        var page = SceneFlowRegistry.Get(pageName);
        return page != null ? page.GetComponent<SoundSelect>() : null;
    }

    // Update is called once per frame
    void Update() {
    }

    public void ResetBodyManager() {
        ResolvePackRefs();
        if (yellowSelect != null && blueSelect != null)
            ResetInstruments();
        playTimes = 0;
        if (auso != null)
        {
            auso.Stop();
            auso.clip = null;
        }
    }

    public AudioSource auso;
    public int song_idx = 0;
    public SoundSelect yellowSelect;
    public SoundSelect blueSelect;
    public readonly int[] yellowPicks = { 0, 0 };
    public readonly int[] bluePicks = { 0, 0 };

    public void CacheInstrumentPicks()
    {
        if (yellowSelect != null && yellowSelect.slots != null)
        {
            for (int i = 0; i < yellowPicks.Length && i < yellowSelect.slots.Length; i++)
                yellowPicks[i] = yellowSelect.slots[i].indexData;
        }

        if (blueSelect != null && blueSelect.slots != null)
        {
            for (int i = 0; i < bluePicks.Length && i < blueSelect.slots.Length; i++)
                bluePicks[i] = blueSelect.slots[i].indexData;
        }

        MBodyDiagLog.Step("Body", $"Cached picks yellow=[{yellowPicks[0]},{yellowPicks[1]}] blue=[{bluePicks[0]},{bluePicks[1]}]");
    }

    public int GetInstrumentIndex(SoundSelect select, int slotIndex)
    {
        if (select != null && select.slots != null && slotIndex < select.slots.Length
            && select.slots[slotIndex].indexData > 0)
            return select.slots[slotIndex].indexData;

        var picks = select != null && select.name.Contains("Blue") ? bluePicks : yellowPicks;
        if (slotIndex >= 0 && slotIndex < picks.Length)
            return picks[slotIndex];

        return 0;
    }
    public AudioClip[] bgms;
    public VideoCaptureCam vcc;
    
    public AudioClip[] preSoundsA;
    
    public AudioClip[] preSoundsB;
    public AudioClip[] DKPreview;
    public AudioClip[] BodyPreview;
    public void ResetInstruments() {
        ResolvePackRefs();
        if (yellowSelect == null || blueSelect == null)
            return;

        DragSlot[] yellowSlots = yellowSelect.slots;
        DragSlot[] blueSlots = blueSelect.slots;
        foreach (var v in yellowSlots) {
            v.backIcons.GetComponent<DropIcon>().ResetPosition();
        }
        foreach (var v in blueSlots) {
            v.backIcons.GetComponent<DropIcon>().ResetPosition();
        }

        yellowPicks[0] = yellowPicks[1] = 0;
        bluePicks[0] = bluePicks[1] = 0;
    }

    public GameObject musicSelect;
    public GameObject EndPage;
    public GameObject endSelect;
    public void EndMainGame() {
        SceneFlowRegistry.Refresh();
        ResolvePackRefs();

        if (musicSelect == null)
            musicSelect = SceneFlowRegistry.Get("MusicSelect");
        if (endSelect == null)
            endSelect = SceneFlowRegistry.Get("EndSelect (1)");
        if (EndPage == null)
            EndPage = SceneFlowRegistry.Get("EndMessage");

        var next = playTimes < 3 ? musicSelect : endSelect;
        if (next == null) {
            MBodyDiagLog.Warn("Body", $"EndMainGame missing next page playTimes={playTimes} music={(musicSelect != null)} end={(endSelect != null)}");
            return;
        }

        MBodyDiagLog.Step("Body", $"EndMainGame playTimes={playTimes} -> {next.name} (id={next.GetInstanceID()})");
        SceneFlowNavigator.ShowOnly(next, EndPage);
        ResetInstruments();
    }

}
