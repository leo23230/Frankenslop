using UnityEngine;

public static class PoseEvaluator
{
    public static PoseEvaluationResult Evaluate(
        PoseTemplate template,
        PoseableLimbIKController controller)
    {
        if (template == null)
        {
            Debug.LogError("Cannot evaluate a null PoseTemplate.");
            return default;
        }

        if (controller == null)
        {
            Debug.LogError(
                "Cannot evaluate a null PoseableLimbIKController."
            );

            return default;
        }

        float leftHandScore =
            CalculatePointScore(
                controller.GetScoringLimbPosition(
                    PoseableLimbAction.LeftArm
                ),
                template.LeftHand
            );

        float rightHandScore =
            CalculatePointScore(
                controller.GetScoringLimbPosition(
                    PoseableLimbAction.RightArm
                ),
                template.RightHand
            );

        float leftFootScore =
            CalculatePointScore(
                controller.GetScoringLimbPosition(
                    PoseableLimbAction.LeftLeg
                ),
                template.LeftFoot
            );

        float rightFootScore =
            CalculatePointScore(
                controller.GetScoringLimbPosition(
                    PoseableLimbAction.RightLeg
                ),
                template.RightFoot
            );

        float spineLeanScore =
            CalculateLeanScore(
                controller.GetScoringLeanDegrees(
                    PoseLeanChannel.Spine
                ),
                template.SpineLean
            );

        float chestLeanScore =
            CalculateLeanScore(
                controller.GetScoringLeanDegrees(
                    PoseLeanChannel.Chest
                ),
                template.ChestLean
            );

        float headLeanScore =
            CalculateLeanScore(
                controller.GetScoringLeanDegrees(
                    PoseLeanChannel.Head
                ),
                template.HeadLean
            );

        float weightedTotal = 0f;
        float totalWeight = 0f;

        AddWeightedScore(
            leftHandScore,
            template.LeftHand.Weight,
            ref weightedTotal,
            ref totalWeight
        );

        AddWeightedScore(
            rightHandScore,
            template.RightHand.Weight,
            ref weightedTotal,
            ref totalWeight
        );

        AddWeightedScore(
            leftFootScore,
            template.LeftFoot.Weight,
            ref weightedTotal,
            ref totalWeight
        );

        AddWeightedScore(
            rightFootScore,
            template.RightFoot.Weight,
            ref weightedTotal,
            ref totalWeight
        );

        AddWeightedScore(
            spineLeanScore,
            template.SpineLean.Weight,
            ref weightedTotal,
            ref totalWeight
        );

        AddWeightedScore(
            chestLeanScore,
            template.ChestLean.Weight,
            ref weightedTotal,
            ref totalWeight
        );

        AddWeightedScore(
            headLeanScore,
            template.HeadLean.Weight,
            ref weightedTotal,
            ref totalWeight
        );

        float overallScore =
            totalWeight > 0f
                ? Mathf.Clamp01(
                    weightedTotal /
                    totalWeight
                )
                : 0f;

        bool requiredComponentsPassed =
            MeetsRequirement(
                leftHandScore,
                template.LeftHand.Required,
                template.LeftHand.MinimumRequiredScore
            ) &&
            MeetsRequirement(
                rightHandScore,
                template.RightHand.Required,
                template.RightHand.MinimumRequiredScore
            ) &&
            MeetsRequirement(
                leftFootScore,
                template.LeftFoot.Required,
                template.LeftFoot.MinimumRequiredScore
            ) &&
            MeetsRequirement(
                rightFootScore,
                template.RightFoot.Required,
                template.RightFoot.MinimumRequiredScore
            ) &&
            MeetsRequirement(
                spineLeanScore,
                template.SpineLean.Required,
                template.SpineLean.MinimumRequiredScore
            ) &&
            MeetsRequirement(
                chestLeanScore,
                template.ChestLean.Required,
                template.ChestLean.MinimumRequiredScore
            ) &&
            MeetsRequirement(
                headLeanScore,
                template.HeadLean.Required,
                template.HeadLean.MinimumRequiredScore
            );

        MinigameScoreGrade overallGrade =
            requiredComponentsPassed
                ? template.OverallThresholds.GetGrade(
                    overallScore
                )
                : MinigameScoreGrade.Fail;

        PoseableLimbAction player1Action =
            controller.GetAssignedAction(
                PlayerSlot.Player1
            );

        PoseableLimbAction player2Action =
            controller.GetAssignedAction(
                PlayerSlot.Player2
            );

        PoseableLimbAction player3Action =
            controller.GetAssignedAction(
                PlayerSlot.Player3
            );

        PoseableLimbAction player4Action =
            controller.GetAssignedAction(
                PlayerSlot.Player4
            );

        return new PoseEvaluationResult
        {
            OverallNormalizedScore = overallScore,
            OverallGrade = overallGrade,
            Passed =
                overallGrade !=
                MinigameScoreGrade.Fail,
            RequiredComponentsPassed =
                requiredComponentsPassed,

            LeftHandScore = leftHandScore,
            RightHandScore = rightHandScore,
            LeftFootScore = leftFootScore,
            RightFootScore = rightFootScore,

            SpineLeanScore = spineLeanScore,
            ChestLeanScore = chestLeanScore,
            HeadLeanScore = headLeanScore,

            Player1 =
                BuildIndividualPlayerScore(
                    PlayerSlot.Player1,
                    player1Action,
                    GetScoreForAction(
                        player1Action,
                        leftHandScore,
                        rightHandScore,
                        leftFootScore,
                        rightFootScore
                    ),
                    template.IndividualThresholds
                ),

            Player2 =
                BuildIndividualPlayerScore(
                    PlayerSlot.Player2,
                    player2Action,
                    GetScoreForAction(
                        player2Action,
                        leftHandScore,
                        rightHandScore,
                        leftFootScore,
                        rightFootScore
                    ),
                    template.IndividualThresholds
                ),

            Player3 =
                BuildIndividualPlayerScore(
                    PlayerSlot.Player3,
                    player3Action,
                    GetScoreForAction(
                        player3Action,
                        leftHandScore,
                        rightHandScore,
                        leftFootScore,
                        rightFootScore
                    ),
                    template.IndividualThresholds
                ),

            Player4 =
                BuildIndividualPlayerScore(
                    PlayerSlot.Player4,
                    player4Action,
                    GetScoreForAction(
                        player4Action,
                        leftHandScore,
                        rightHandScore,
                        leftFootScore,
                        rightFootScore
                    ),
                    template.IndividualThresholds
                )
        };
    }

    private static float CalculatePointScore(
        Vector2 current,
        PosePoint target)
    {
        Vector2 difference =
            current -
            target.Position;

        float toleranceX =
            Mathf.Max(
                0.001f,
                target.Tolerance.x
            );

        float toleranceY =
            Mathf.Max(
                0.001f,
                target.Tolerance.y
            );

        float normalizedDistance =
            Mathf.Sqrt(
                Mathf.Pow(
                    difference.x /
                    toleranceX,
                    2f
                ) +
                Mathf.Pow(
                    difference.y /
                    toleranceY,
                    2f
                )
            );

        return Mathf.Clamp01(
            1f -
            normalizedDistance
        );
    }

    private static float CalculateLeanScore(
        float currentDegrees,
        PoseLeanPoint target)
    {
        float angularError =
            Mathf.Abs(
                Mathf.DeltaAngle(
                    currentDegrees,
                    target.TargetDegrees
                )
            );

        return Mathf.Clamp01(
            1f -
            angularError /
            Mathf.Max(
                0.01f,
                target.ToleranceDegrees
            )
        );
    }

    private static void AddWeightedScore(
        float score,
        float weight,
        ref float weightedTotal,
        ref float totalWeight)
    {
        float safeWeight =
            Mathf.Max(
                0f,
                weight
            );

        weightedTotal +=
            Mathf.Clamp01(score) *
            safeWeight;

        totalWeight +=
            safeWeight;
    }

    private static bool MeetsRequirement(
        float score,
        bool required,
        float minimumRequiredScore)
    {
        return !required ||
               score >=
               minimumRequiredScore;
    }

    private static float GetScoreForAction(
        PoseableLimbAction action,
        float leftHandScore,
        float rightHandScore,
        float leftFootScore,
        float rightFootScore)
    {
        return action switch
        {
            PoseableLimbAction.LeftArm =>
                leftHandScore,

            PoseableLimbAction.RightArm =>
                rightHandScore,

            PoseableLimbAction.LeftLeg =>
                leftFootScore,

            PoseableLimbAction.RightLeg =>
                rightFootScore,

            _ =>
                0f
        };
    }

    private static IndividualPlayerScore
        BuildIndividualPlayerScore(
            PlayerSlot slot,
            PoseableLimbAction action,
            float limbScore,
            MinigameScoreThresholds thresholds)
    {
        bool wasEvaluated =
            action !=
            PoseableLimbAction.None;

        float normalizedScore =
            wasEvaluated
                ? Mathf.Clamp01(
                    limbScore
                )
                : 0f;

        return new IndividualPlayerScore
        {
            PlayerSlot = slot,
            ControlledAction = action,
            NormalizedScore = normalizedScore,
            Grade =
                wasEvaluated
                    ? thresholds.GetGrade(
                        normalizedScore
                    )
                    : MinigameScoreGrade.Fail,
            WasEvaluated = wasEvaluated
        };
    }
}
