using UnityEngine;

[CreateAssetMenu(
    fileName = "PoseTemplate",
    menuName = "Pose Game/Pose Template"
)]
public class PoseTemplate : ScriptableObject
{
    [Header("Four Main IK Targets")]
    [SerializeField] private PosePoint leftHand;
    [SerializeField] private PosePoint rightHand;
    [SerializeField] private PosePoint leftFoot;
    [SerializeField] private PosePoint rightFoot;

    [Header("Procedural Body Lean")]
    [SerializeField] private PoseLeanPoint spineLean;
    [SerializeField] private PoseLeanPoint chestLean;
    [SerializeField] private PoseLeanPoint headLean;

    [Header("Overall Cooperative Grade Ranges")]
    [SerializeField] private MinigameScoreThresholds overallThresholds;

    [Header("Hidden Individual Feedback Ranges")]
    [SerializeField] private MinigameScoreThresholds individualThresholds;

    public PosePoint LeftHand => leftHand;
    public PosePoint RightHand => rightHand;
    public PosePoint LeftFoot => leftFoot;
    public PosePoint RightFoot => rightFoot;

    public PoseLeanPoint SpineLean => spineLean;
    public PoseLeanPoint ChestLean => chestLean;
    public PoseLeanPoint HeadLean => headLean;

    public MinigameScoreThresholds OverallThresholds =>
        overallThresholds;

    public MinigameScoreThresholds IndividualThresholds =>
        individualThresholds;

    public void SetCapturedPose(
        Vector2 leftHandPosition,
        Vector2 rightHandPosition,
        Vector2 leftFootPosition,
        Vector2 rightFootPosition,
        float spineLeanDegrees,
        float chestLeanDegrees,
        float headLeanDegrees)
    {
        leftHand.SetCapturedPosition(leftHandPosition);
        rightHand.SetCapturedPosition(rightHandPosition);
        leftFoot.SetCapturedPosition(leftFootPosition);
        rightFoot.SetCapturedPosition(rightFootPosition);

        spineLean.SetCapturedDegrees(spineLeanDegrees);
        chestLean.SetCapturedDegrees(chestLeanDegrees);
        headLean.SetCapturedDegrees(headLeanDegrees);
    }

    public void InitializeDefaults(
        Vector2 defaultHandTolerance,
        Vector2 defaultFootTolerance,
        float defaultLeanTolerance,
        float limbWeight,
        float leanWeight,
        MinigameScoreThresholds newOverallThresholds,
        MinigameScoreThresholds newIndividualThresholds)
    {
        leftHand = PosePoint.Create(
            Vector2.zero,
            defaultHandTolerance,
            limbWeight
        );

        rightHand = PosePoint.Create(
            Vector2.zero,
            defaultHandTolerance,
            limbWeight
        );

        leftFoot = PosePoint.Create(
            Vector2.zero,
            defaultFootTolerance,
            limbWeight
        );

        rightFoot = PosePoint.Create(
            Vector2.zero,
            defaultFootTolerance,
            limbWeight
        );

        spineLean = PoseLeanPoint.Create(
            0f,
            defaultLeanTolerance,
            leanWeight
        );

        chestLean = PoseLeanPoint.Create(
            0f,
            defaultLeanTolerance,
            leanWeight
        );

        headLean = PoseLeanPoint.Create(
            0f,
            defaultLeanTolerance,
            leanWeight
        );

        overallThresholds = newOverallThresholds;
        individualThresholds = newIndividualThresholds;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!overallThresholds.HasValidOrdering())
        {
            Debug.LogWarning(
                $"Overall score thresholds are out of order on {name}. " +
                "Expected Pass <= Good <= Perfect.",
                this
            );
        }

        if (!individualThresholds.HasValidOrdering())
        {
            Debug.LogWarning(
                $"Individual score thresholds are out of order on {name}. " +
                "Expected Pass <= Good <= Perfect.",
                this
            );
        }
    }
#endif
}
