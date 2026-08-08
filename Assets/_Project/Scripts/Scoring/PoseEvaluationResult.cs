using System;

[Serializable]
public struct IndividualPlayerScore
{
    public PlayerSlot PlayerSlot;
    public PoseableLimbAction ControlledAction;
    public float NormalizedScore;
    public MinigameScoreGrade Grade;
    public bool WasEvaluated;
}

[Serializable]
public struct PoseEvaluationResult
{
    public float OverallNormalizedScore;
    public MinigameScoreGrade OverallGrade;
    public bool Passed;
    public bool RequiredComponentsPassed;

    public float LeftHandScore;
    public float RightHandScore;
    public float LeftFootScore;
    public float RightFootScore;

    public float SpineLeanScore;
    public float ChestLeanScore;
    public float HeadLeanScore;

    public IndividualPlayerScore Player1;
    public IndividualPlayerScore Player2;
    public IndividualPlayerScore Player3;
    public IndividualPlayerScore Player4;

    public IndividualPlayerScore GetPlayerScore(
        PlayerSlot slot)
    {
        return slot switch
        {
            PlayerSlot.Player1 => Player1,
            PlayerSlot.Player2 => Player2,
            PlayerSlot.Player3 => Player3,
            PlayerSlot.Player4 => Player4,
            _ => default
        };
    }
}
