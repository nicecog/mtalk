using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Stage2PositiveFeedback : MonoBehaviour
{
    private void OnEnable() {
        resetStrings();
        normal = text.color;
        text.gameObject.SetActive(false);
        StartCoroutine(ActTask());
        
    }

    private int[] target = {
        2,8,3,6,0,
        3,0,1,8,9,
        7,2,5,8,2,
        6,4,2,7,0,
        5,8,5,1,4,
        3,8,1,0,7,
        1,9
    };
    public Color normal;
    public Color emphasis;
    IEnumerator ActTask() {
        NextButton.gameObject.SetActive(false);
        int temp = target[s2m.scenesToDo];
        //con.playNumber(0);
        //text.text = Messages[temp-1];
        float boxHeight = text.rectTransform.rect.height;
        float height = boxHeight*Messages[temp].Count;
        float centerY = text.rectTransform.rect.y;
        Text[] lines = new Text[Messages[temp].Count];
        for (int i = 0; i < Messages[temp].Count; i++) {
            lines[i] = GameObject.Instantiate(text,text.transform.position,text.transform.rotation,text.transform.parent);
            lines[i].gameObject.SetActive(true);
            lines[i].text = Messages[temp][i];
            lines[i].rectTransform.anchoredPosition =
                text.rectTransform.anchoredPosition+ new Vector2(0, centerY + (height / 2) - i * boxHeight);
        }

        yield return new WaitForSeconds(1f);
        con.playNumber(temp+1);
        
        float startTime = Time.fixedTime;
        while (true) {
            yield return new WaitForEndOfFrame();
            float now = Time.fixedTime;
            float t = con.asc.time;
            int cnt = timepoints[temp].Count;
            for (int i = 0;i<cnt; i++) {
                lines[i].color = normal;
            }
            for (int i = cnt-1;i>=0; i--) {
                if (now-startTime > timepoints[temp][i]) {
                    lines[i].color = emphasis;
                    break;
                }
            }

            if (!con.asc.isPlaying && now > startTime + 10f)
                break;
            
        }
        Debug.Log("End");
        //while (con.asc.isPlaying)
        //    yield return new WaitForEndOfFrame();
        
        
        //yield return new WaitForSeconds(1f);
        
        yield return new WaitForSeconds(1f);
        for(int i=0;i<lines.Length;i++)
            Destroy(lines[i].gameObject);
        NextButton.gameObject.SetActive(true);
    }

    private List<string>[] Messages;
    private List<float>[] timepoints;
    private void resetStrings() {
        Messages = new List<string>[10];
        timepoints = new List<float>[10];
        
        Messages[0] = new List<string>() {
            "우정은 함께하는 것",
            "우정은 배려하는 것",
            "소중한 우리 우정을 지켜보아요",
            "우정은 함께하는 것",
            "우정은 배려하는 것",
            "소중한 우리 시간을 가꿔나가요",
"",
            "때론 하고 싶은 것이 다르고",
            "때론 의견이 달라 속상해도",
            "서로의 마음 배려하면서 대화나눠요",
            "때론 실수를 하기도 하고",
            "어쩔 수 없는 상황 생겨도",
            "서로의 마음 배려하면서 해결해봐요"

        };
        timepoints[0] = new List<float>(){2f, 5f, 8f, 14f, 16f, 19f, 25f,27f,31f,33f,39f,41f,45f};
        Messages[1] = new List<string>()
            {
                "사람들 많은 장소에서 질서를 지켜요",
                "서로를 배려하면서",
                "사람들 많은 장소에서 에티켓 지켜요",
                "예절을 지켜봐요",

                "",
                "감사합니다, 실례합니다",
                "친절하게 말해요",
                "안녕하세요, 힘을 내세요",
                "함께라서 기뻐요",
                "사람들 많은 장소에서 질서를 지켜요",
                "서로를 배려하면서",
                "사람들 많은 장소에서 에티켓 지켜요",
                "예절을 지켜봐요"

            };
        timepoints[1] = new List<float>(){6f,11f,14f,19f,21f,22f,26f,30f,34f,37f,42f,45f,49f};
        
        Messages[2] = new List<string>() {
            "속상하고 불편한 마음이 들 때",
            "계획대로 되지 않고 짜증 날 때 ",
            "깊이 숨 쉬며 생각해봐요  ",
            "다른 생각하며 기다려 봐요",
            "차분하게 내 마음 전해요",
            "좋은 방법이 떠오를 거에요",
            "우리 모두 오늘도 파이팅!"

        };
        timepoints[2] = new List<float>() { 7f,12f,16f,21f,26f,30f,34f};
        
        Messages[3] = new List<string>() {
            "누구든 살다 보면 실수를 하지  ",
            "마음대로 되지 않아 짜증나지",
            "어떻게 한다면 실수 안 할까",
            "어떻게 하면 될까",
            "괜찮아요 누구나 실수하니까",
            "실수 덕분에 차츰 잘하게 되죠",
            "모든 걸 잘하는 사람은 없죠 ",
            "랄랄랄랄랄랄 힘내요!",

            "",
            "누구든 살다 보면 실수를 하지",
            "마음대로 되지 않아 힘도 들지 ",
            "우린 실수 때문에 포기하지 않아",
            "랄랄랄랄라 힘내자!",
            "랄랄랄랄라 화이팅!"

        };
        timepoints[3] = new List<float>(){13f,18f,24f,29f,35f,41f,46f,52f,58f,59f,64f,70f,75f,81f};

        Messages[4] = new List<string>() {
            "가끔은 짓궂고 놀리는 사람 있죠",
            "날 무시하거나 거친 행동도 하죠",
            "신경쓰지않고 가만히 있어봐도",
            "나아지지않고 괴롭힐 때도있죠",
            "그러지마 그럼 난 너무나 속상해",
            "내 기분 솔직히 얘기해",
            "힘있는 목소리로 당당히 말해요", 
            "그렇게 하지마",
            "말로 안되면 도움 받아요",
            "선생님 저 좀 도와줘요",
            "모두 소중하듯 나도 소중해요 목소릴 내어봐요!",
            "목소릴 내어봐요!",
            "그러지마 그럼 난 너무나 속상해", 
            "내 기분 솔직히 얘기해",
            "힘있는 목소리로 당당히 말해요",
            "그렇게 하지마"

        };
        timepoints[4] = new List<float>(){8f,12f,16f,21f,25f,29f,33f,38f,42f,46f,50f,54f,58f,63f,67f,71f};

        Messages[5] = new List<string>() {
            "원하는게 있다면", 
            "정중하게 부탁 해보세요",
            "내맘대로 하기보다", 
            "대화를 통해 의견 조정해요",  
            "상대의 말을 잘 들어보세요", 
            "대답하기 전에 생각해봐요",
            "이해가 안 가면 질문을 해요",
            "최대한 기분 좋게 얘기해요",

            "",
            "원하는게 있다면",
            "정중하게 부탁 해보세요",
            "내맘대로 하기보다",
            "대화를 통해 의견 조정해요"

        };
        timepoints[5] = new List<float>(){ 4f,7f,11f,15f,20f,24f,28f,31f,35f,37f,41f,45f,49f};

        Messages[6] = new List<string>() {
            "나의 것이 아닌 것 좋아 보여도",
            "다른 사람 물건이 맘에 들어도",
            "내 것 지키고 싶듯 다른 이의 물건을",
            "나도 아껴주면서 존중해줘요",

            "",
            "내 생각과 다르고 틀려 보여도",
            "내 의견이 아무리 맞다 하여도",
            "내 의견 중요하듯 다른 이의 의견을",
            "나도 아껴주면서 존중해줘요"

        };
        timepoints[6] = new List<float>(){7f,12f,16f,20f,25f,33f,38f,42f,47f};

        Messages[7] = new List<string>() {
            "약속을 잘 지킨다는 건 ",
            "서로 존중함을 말하죠",
            "시간도 (잘) 약속도 잘 지켜요",
            "약속을 잘 지킨다는건", 
            "책임을 다함을 말하죠",
            "내 멋대로 바꾸거나", 
            "없던 걸로 하지 않아요",
            "만약에 못 지킨다면", 
            "솔직하게 설명 해주세요",
            "지켜야하는 보물이랍니다", 
            "약!속!"
        };
        timepoints[7] = new List<float>(){6f,10f,14f,21f,25f,29f,32f,36f,39f,43f,49f};
        Messages[8] = new List<string>() {
            "어떻게 할까 고민될 때,",
            "내 마음대로 되지 않을 때",
            "속상하고 짜증나도",
            "무엇보다 중요한 건",
            "입장 바꿔 생각해봐요",
            "이해하고 배려해 줘요",
            "누군가 힘들어 하나요?", 
            "고민이 있어 보이나요?",
            "해결책을 주기 전에",
            "무엇보다 중요한 건",
            "우선 그 마음 알아줘요",
            "힘든 마음 위로해 줘요"

        };
        timepoints[8] = new List<float>(){8f,18f,27f,32f,36f,45f,54f,63f,72f,77f,81f,91f};
        Messages[9] = new List<string>() {
            "안녕하세요", 
            "반갑게 인사해요",
            "처음 만날 땐 나를 소개 해봐요",
            "반가운 인사로 마음 활짝 열어요",
            "소개를 하면서 서로를 알아가요",    
            "서두르지 말고 천천히, 천천히",
            "함께 하는 시간 즐겨요, 즐겨요",
            "서로 눈 맞추며 편안하게, 편안하게",
            "부드러운 용기로 인사 건네며",
            "친구를 만들어요!"

        };
        timepoints[9] = new List<float>(){7f,9f,14f,21f,29f,35f,42f,49f,57f,63f};

    }

    public Button NextButton;
    public Text text;
    public AudioController con;
    public Stage2Manager s2m;
}
