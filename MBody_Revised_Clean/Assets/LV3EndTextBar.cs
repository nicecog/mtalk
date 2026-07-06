using UnityEngine;
using UnityEngine.UI;

public class LV3EndTextBar : MonoBehaviour
{
    public DanceStarManager dm;
    public GameObject jump;
    Text txt;

    void OnEnable() {
        txt = GetComponent<Text>();
        if (dm == null)
            dm = FindFirstObjectByType<DanceStarManager>(FindObjectsInactive.Include);

        if (jump != null)
            jump.SetActive(false);

        if (dm == null) {
            if (txt != null)
                txt.text = "댄스 스타 도전 메뉴";
            return;
        }

        // Stage 3 must not reuse the 1/2-stage retry menu.
        if (dm.GameLevel >= 3) {
            if (txt != null)
                txt.text = "댄스 스타 도전을 완료했습니다.";
            var thirdEnd = SceneFlowRegistry.Get("LV3ThirdEnd");
            if (thirdEnd != null) {
                MBodyDiagLog.Step("LV3End", "GameLevel>=3 redirect -> LV3ThirdEnd");
                SceneFlowNavigator.ShowOnly(thirdEnd, transform.root != null
                    ? FindSelfPage()
                    : gameObject);
            }
            return;
        }

        if (txt != null)
            txt.text = "댄스 스타 " + dm.GameLevel + "단계가 쉬웠으면 다음 단계 도전,\n어려웠으면 재도전을 선택해 주세요.";
        if (jump != null)
            jump.SetActive(true);
    }

    GameObject FindSelfPage() {
        var t = transform;
        while (t != null) {
            if (t.CompareTag("SceneFlow"))
                return t.gameObject;
            t = t.parent;
        }
        return gameObject;
    }
}
