using UnityEngine;

/// <summary>Page 루트 — 씬 분리 후 MSocialPageRegistry 등록.</summary>
public class MSocialPageMarker : MonoBehaviour
{
    public MSocialPageId pageId;

    void OnEnable() => MSocialPageRegistry.Register(pageId, gameObject);

    void OnDisable() => MSocialPageRegistry.Unregister(pageId, gameObject);

    public static MSocialPageId FromLegacyName(string name)
    {
        switch (name)
        {
            case "Background": return MSocialPageId.Background;
            case "1. IntroLogo": return MSocialPageId.IntroLogo;
            case "2. LoginPage": return MSocialPageId.Login;
            case "3. IntroMovie": return MSocialPageId.IntroMovie;
            case "4. Stage1Start": return MSocialPageId.Stage1Start;
            case "5. Stage1Intro": return MSocialPageId.Stage1Intro;
            case "6. Stage1SelectMap": return MSocialPageId.Stage1SelectMap;
            case "7. Stage1SelectScene": return MSocialPageId.Stage1SelectScene;
            case "8. Stage1WatchScene": return MSocialPageId.Stage1WatchScene;
            case "9-1. Stage1Positive": return MSocialPageId.Stage1Positive;
            case "9-2. Stage1Negative": return MSocialPageId.Stage1Negative;
            case "9-3. Stage1Feedback": return MSocialPageId.Stage1Feedback;
            case "10. Stage1End": return MSocialPageId.Stage1End;
            case "11. Stage2Start": return MSocialPageId.Stage2Start;
            case "12. Stage2Intro": return MSocialPageId.Stage2Intro;
            case "13. Stage2Select": return MSocialPageId.Stage2Select;
            case "14. Stage2WatchScene": return MSocialPageId.Stage2WatchScene;
            case "15. ActSelect": return MSocialPageId.ActSelect;
            case "16. ResultPositive": return MSocialPageId.ResultPositive;
            case "16-2. ResultNegative": return MSocialPageId.ResultNegative;
            case "17. PositiveFeedback": return MSocialPageId.PositiveFeedback;
            case "18. EndPage": return MSocialPageId.EndPage;
            default: return MSocialPageId.None;
        }
    }
}
