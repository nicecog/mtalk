using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// FER+ 8클래스 → M-Face 4클래스(Happy/Neutral/Sad/Anger) 매핑 및 기존 서버 포맷 호환.
/// </summary>
public static class MFaceEmotionMapper
{
    // FER+ 순서: neutral, happiness, surprise, sadness, anger, disgust, fear, contempt
    public static readonly string[] FerPlusLabels =
    {
        "neutral", "happiness", "surprise", "sadness", "anger", "disgust", "fear", "contempt"
    };

    public struct FerPlusScores
    {
        public float neutral;
        public float happiness;
        public float surprise;
        public float sadness;
        public float anger;
        public float disgust;
        public float fear;
        public float contempt;
    }

    public static FerPlusScores FromLogits(float[] logits)
    {
        if (logits == null || logits.Length < 8)
            throw new ArgumentException("FER+ expects 8 logits");

        var probs = Softmax(logits);
        return new FerPlusScores
        {
            neutral = probs[0],
            happiness = probs[1],
            surprise = probs[2],
            sadness = probs[3],
            anger = probs[4],
            disgust = probs[5],
            fear = probs[6],
            contempt = probs[7]
        };
    }

    /// <summary>기존 Emotion 클래스 + M-Face emotion int (0~3, -1 실패).</summary>
    public static int ClassifyMFace(Emotion em, int correctType, float thr)
    {
        if (em.Happy >= em.Anger && em.Happy >= em.Sad && em.Happy >= em.Neutral)
        {
            // continue
        }

        int emotion;
        if (em.Happy >= em.Anger && em.Happy >= em.Sad && em.Happy >= em.Neutral)
            emotion = 0;
        else if (em.Neutral >= em.Happy && em.Neutral >= em.Sad &&
                 em.Neutral >= em.Anger && em.Anger <= thr * 1.5f && em.Sad <= thr * 1.5f)
            emotion = 1;
        else if (em.Sad >= em.Happy && em.Sad >= em.Neutral && em.Sad >= em.Anger)
            emotion = 2;
        else if (em.Anger >= em.Happy && em.Anger >= em.Neutral && em.Anger >= em.Sad)
            emotion = 3;
        else
            emotion = -1;

        if (correctType == 0 && em.Happy >= thr) emotion = 0;
        else if (correctType == 1 && em.Neutral >= thr && em.Anger <= thr * 1.5f && em.Sad <= thr * 1.5f) emotion = 1;
        else if (correctType == 2 && em.Sad >= thr) emotion = 2;
        else if (correctType == 3 && em.Anger >= thr) emotion = 3;

        return emotion;
    }

    public static Emotion ToLegacyEmotion(FerPlusScores s)
    {
        // M-Face Emotion: Neutral, Happy, Sad, Surprise, Fear, Disgust, Anger
        float surprise = s.surprise;
        float fear = s.fear;
        float disgust = s.disgust;
        return new Emotion
        {
            Neutral = s.neutral,
            Happy = s.happiness,
            Sad = s.sadness + s.fear * 0.25f,
            Surprise = surprise,
            Fear = fear,
            Disgust = disgust,
            Anger = s.anger + s.disgust * 0.25f + s.contempt * 0.15f
        };
    }

    /// <summary>서버 buildStubEmotion() 과 동일 포맷 문자열.</summary>
    public static string ToServerEmotionString(Emotion em)
    {
        return string.Format(CultureInfo.InvariantCulture,
            "Neutral:{0:F2},Happy:{1:F2},Sad:{2:F2},Surprise:{3:F2},Fear:{4:F2},Disgust:{5:F2},Anger:{6:F2}",
            em.Neutral, em.Happy, em.Sad, em.Surprise, em.Fear, em.Disgust, em.Anger);
    }

    static float[] Softmax(float[] logits)
    {
        float max = logits[0];
        for (int i = 1; i < logits.Length; i++)
            if (logits[i] > max) max = logits[i];

        var exps = new float[logits.Length];
        float sum = 0f;
        for (int i = 0; i < logits.Length; i++)
        {
            exps[i] = Mathf.Exp(logits[i] - max);
            sum += exps[i];
        }

        for (int i = 0; i < exps.Length; i++)
            exps[i] /= sum;
        return exps;
    }
}
