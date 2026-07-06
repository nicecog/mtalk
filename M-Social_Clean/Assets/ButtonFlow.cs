using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonFlow : MonoBehaviour
{
    public GameObject SelfPage;
    public GameObject NextPage;

    public void ClickNext() {
        if (isWaitingForVoice) {
            try {
                AudioController ac = GetComponent<AudioController>();
                if (ac.voiceOn)
                    return;
                NavigateNext();
            }
            catch {
                NavigateNext();
            }
        }
        else {
            NavigateNext();
        }
    }

    void NavigateNext() {
        if (MSocialSceneRouter.TryResolvePage(NextPage, out var nextId) && nextId != MSocialPageId.None)
        {
            if (nextId == MSocialPageId.Stage1Start)
                StartCoroutine(GoStage1Start());
            else if (SceneFor(nextId) == MSocialSceneId.Stage2)
                StartCoroutine(GoStage2Page(nextId));
            else
                StartCoroutine(MSocialSceneRouter.ShowPage(nextId, this));
            return;
        }

        if (SelfPage != null && MSocialPageMarker.FromLegacyName(SelfPage.name) == MSocialPageId.IntroMovie)
        {
            StartCoroutine(GoStage1Start());
            return;
        }

        if (SelfPage != null)
            SelfPage.SetActive(false);
        if (NextPage != null)
            NextPage.SetActive(true);
    }

    static MSocialSceneId SceneFor(MSocialPageId id) => MSocialSceneRouter.SceneForPage(id);

    IEnumerator GoStage1Start() {
        yield return MSocialSceneRouter.EnsureLoaded(MSocialSceneId.Stage1);
        yield return MSocialSceneRouter.ShowPage(MSocialPageId.Stage1Start, this);
    }

    IEnumerator GoStage2Page(MSocialPageId pageId) {
        yield return MSocialSceneRouter.EnsureLoaded(MSocialSceneId.Stage2);
        yield return MSocialSceneRouter.ShowPage(pageId, this);
    }

    public bool isWaitingForVoice = false;
}
