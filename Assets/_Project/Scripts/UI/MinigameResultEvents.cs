using System;

public static class MinigameResultEvents
{
    public static event Action<MinigameScoreGrade,float> OverallResultPresented;

    public static void RaiseOverallResult(MinigameScoreGrade grade,float normalizedScore)
    {
        OverallResultPresented?.Invoke(grade,normalizedScore);
    }
}
