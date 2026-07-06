using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Central Korean UI strings (UTF-8). Scene YAML text is fine; runtime strings in legacy .cs were corrupted.
/// </summary>
public static class MBodyKoreanText
{
    public static readonly string[] InstrumentPickFeedback =
    {
        "탁월한 선택이에요!",
        "오! 느낌이 좋아요!",
        "잘 어울릴 것 같아요!",
        "최고의 선택이에요!",
        "멋진 연주가 기대돼요!",
        "감각적인 선택이에요!",
        "음악에 좀 아는군요!",
        "악기에 좀 아는군요!",
        "짝짝짝! 좋은 선택이에요!",
        "숙련된 선택이에요!",
        "음악에 잘 맞아요!",
        "정말 최고예요!",
        "와~ 멋진 조합이에요!",
    };

    public static string InstrumentPickComplete(bool isBlue)
    {
        return isBlue ? "블루 악기 선택 완료!" : "오렌지 악기 선택 완료!";
    }

    public static string BodyScoreMessage(int points)
    {
        return "멋진 연주로 " + points + " 포인트를 받았어요!";
    }

    public static string DanceSectionIntro =>
        "음악의 시작이 바뀌는 부분을 위에 그림을 보며 미리 연습해요!!";

    public static string DanceSectionPlay =>
        "음악이 바뀔 때마다 발 움직임과 상체 움직임을 바꿔가며 춤춰보세요!!";

    public static string GetDanceName(int danceNum)
    {
        switch (danceNum)
        {
            case 1: return "날아봐";
            case 2: return "날봐줘";
            case 3: return "디스코";
            case 4: return "연주";
            case 5: return "스웨그";
            case 6: return "풍차";
            default: return "댄스";
        }
    }

    public static string DancePracticeModeText(int danceNum, bool isSoft)
    {
        var mode = isSoft ? "소프트" : "파워";
        return GetDanceName(danceNum) + " 댄스의 " + mode + " 모드입니다.";
    }

    public static string DancePreviewIntro(int danceNum)
    {
        return "댄스 미리 보기! 원하는 " + GetDanceName(danceNum)
            + " 댄스를 선택해 주세요.\n소프트와 파워 중 하나를 골라 미리 볼 수 있어요.";
    }

    static Font cachedFont;

    public static void EnsureKoreanFont(Text text)
    {
        if (text == null || text.font != null)
            return;

        if (cachedFont == null)
        {
            var sample = Object.FindFirstObjectByType<Text>(FindObjectsInactive.Include);
            if (sample != null && sample.font != null)
                cachedFont = sample.font;
        }

        if (cachedFont != null)
            text.font = cachedFont;
    }
}
