using System;
using UnityEngine;

[Serializable]
public struct MinigameScoreThresholds
{
    [Range(0f, 1f)]
    [SerializeField] private float passMinimum;

    [Range(0f, 1f)]
    [SerializeField] private float goodMinimum;

    [Range(0f, 1f)]
    [SerializeField] private float perfectMinimum;

    public float PassMinimum => passMinimum;
    public float GoodMinimum => goodMinimum;
    public float PerfectMinimum => perfectMinimum;

    public MinigameScoreGrade GetGrade(float normalizedScore)
    {
        normalizedScore = Mathf.Clamp01(normalizedScore);

        if (normalizedScore >= perfectMinimum)
            return MinigameScoreGrade.Perfect;

        if (normalizedScore >= goodMinimum)
            return MinigameScoreGrade.Good;

        if (normalizedScore >= passMinimum)
            return MinigameScoreGrade.Pass;

        return MinigameScoreGrade.Fail;
    }

    public bool HasValidOrdering()
    {
        return passMinimum <= goodMinimum &&
               goodMinimum <= perfectMinimum;
    }

    public static MinigameScoreThresholds Create(
        float pass,
        float good,
        float perfect)
    {
        return new MinigameScoreThresholds
        {
            passMinimum = Mathf.Clamp01(pass),
            goodMinimum = Mathf.Clamp01(good),
            perfectMinimum = Mathf.Clamp01(perfect)
        };
    }
}
