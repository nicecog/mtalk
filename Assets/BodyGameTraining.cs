using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

public class BodyGameTraining : MonoBehaviour {
    private void OnEnable() {
        Effect.enabled = false;
        Progress.fillAmount = 0f;
        ProgressBackground.SetActive(true);
        introduction.text = "손을 뻗어 깜빡이는 악기를 두드려보세요.";
        StartCoroutine(PracticeSequence());
    }

    public GameObject ProgressBackground;
    public Text introduction;
    
    private IEnumerator PracticeSequence() {
        introduction.transform.parent.gameObject.SetActive(true);
        ProgressBackground.SetActive(false);
        int[] firstseq = new int[] {0, 1, 2, 3};
        _played = -1;
        for (int i = 0; i < 4; i++) {
            float EffectGlow = Time.fixedTime;
            
            Effect.rectTransform.anchoredPosition = bgs.targets[firstseq[i]].rectTransform.anchoredPosition;
            Effect.enabled = true;
            while (true) {
                float nowTime = Time.fixedTime;
                if (nowTime >= EffectGlow + 2f)
                    EffectGlow = nowTime;
                if (nowTime >= EffectGlow + 1f)
                    Effect.color = new Color(1f, 1f, 1f, 1f - (nowTime - EffectGlow-1));
                else
                    Effect.color = new Color(1f, 1f, 1f, nowTime - EffectGlow);
                

                if (_played >= 0) {

                    if(_played==firstseq[i])
                        break;
                }

                _played = -1;
                yield return null;
            }
        }

        introduction.text = "잘 하셨습니다.";
        Effect.enabled = false;
        yield return new WaitForSecondsRealtime(3f);
        int[] seq = new int[4] ;
        for(int i=0;i<4;i++) {
            seq[i] = i;
        }
        
        introduction.text = "한 번에 한 칸씩 연주합니다.";
        yield return new WaitForSecondsRealtime(3f);
        introduction.transform.parent.gameObject.SetActive(false);
        ProgressBackground.SetActive(true);
        _played = -1;
        for (int i = 0; i < 4; i++) {
            _played = -1;
            Effect.rectTransform.anchoredPosition = bgs.targets[seq[i]].rectTransform.anchoredPosition;
            Effect.enabled = true;
            float EffectGlow = Time.fixedTime;
            while (true) {
                float nowTime = Time.fixedTime;
                if (nowTime >= EffectGlow + 2f)
                    EffectGlow = nowTime;
                if (nowTime >= EffectGlow + 1f)
                    Effect.color = new Color(1f, 1f, 1f, 1f - (nowTime - EffectGlow-1));
                else
                    Effect.color = new Color(1f, 1f, 1f, nowTime - EffectGlow);
                
                if (_played >= 0) {
                    if (_played == seq[i]) {
                        break;
                    }
                    _played = -1;
                }

                yield return null;
            }

            Effect.enabled = false;
            Progress.fillAmount = (i+1)/4f;
            yield return new WaitForSecondsRealtime(1f);
        }

        Effect.enabled = false;
        
            yield return  new WaitForSecondsRealtime(2f);
            NextButton.onClick.Invoke();
        
    }

    private int _played;
    public Button NextButton;
    public void Played(int _count) {
        _played = _count;
    }

    public BodyGameScene bgs;

    public Image Effect;
    public Image Progress;
}
