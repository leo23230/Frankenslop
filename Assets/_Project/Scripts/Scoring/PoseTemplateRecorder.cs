using UnityEngine;

public class PoseTemplateRecorder : MonoBehaviour
{
    [Header("Capture Source")]
    [SerializeField]
    private PoseableLimbIKController controller;

    [Header("New Asset Defaults")]
    [SerializeField]
    private Vector2 defaultHandTolerance =
        new(0.18f, 0.14f);

    [SerializeField]
    private Vector2 defaultFootTolerance =
        new(0.14f, 0.10f);

    [SerializeField, Min(0.01f)]
    private float defaultLeanTolerance = 8f;

    [SerializeField, Min(0f)]
    private float defaultLimbWeight = 1f;

    [SerializeField, Min(0f)]
    private float defaultLeanWeight = 0.5f;

    [Header("Default Overall Thresholds")]
    [Range(0f, 1f)]
    [SerializeField]
    private float defaultPassMinimum = 0.65f;

    [Range(0f, 1f)]
    [SerializeField]
    private float defaultGoodMinimum = 0.82f;

    [Range(0f, 1f)]
    [SerializeField]
    private float defaultPerfectMinimum = 0.95f;

    [Header("Default Individual Thresholds")]
    [Range(0f, 1f)]
    [SerializeField]
    private float defaultIndividualPassMinimum = 0.5f;

    [Range(0f, 1f)]
    [SerializeField]
    private float defaultIndividualGoodMinimum = 0.72f;

    [Range(0f, 1f)]
    [SerializeField]
    private float defaultIndividualPerfectMinimum = 0.9f;

    public PoseableLimbIKController Controller =>
        controller;

    public Vector2 DefaultHandTolerance =>
        defaultHandTolerance;

    public Vector2 DefaultFootTolerance =>
        defaultFootTolerance;

    public float DefaultLeanTolerance =>
        defaultLeanTolerance;

    public float DefaultLimbWeight =>
        defaultLimbWeight;

    public float DefaultLeanWeight =>
        defaultLeanWeight;

    public MinigameScoreThresholds
        GetDefaultOverallThresholds()
    {
        return MinigameScoreThresholds.Create(
            defaultPassMinimum,
            defaultGoodMinimum,
            defaultPerfectMinimum
        );
    }

    public MinigameScoreThresholds
        GetDefaultIndividualThresholds()
    {
        return MinigameScoreThresholds.Create(
            defaultIndividualPassMinimum,
            defaultIndividualGoodMinimum,
            defaultIndividualPerfectMinimum
        );
    }

    public bool TryCapture(
        out PoseCaptureSnapshot snapshot)
    {
        snapshot = default;

        if (controller == null)
        {
            Debug.LogError(
                "PoseTemplateRecorder has no controller assigned.",
                this
            );

            return false;
        }

        if (!controller.HasScoringState)
        {
            Debug.LogWarning(
                "The pose controller has not initialized its " +
                "authoritative scoring state yet.",
                this
            );

            return false;
        }

        snapshot = new PoseCaptureSnapshot
        {
            LeftHand =
                controller.GetScoringLimbPosition(
                    PoseableLimbAction.LeftArm
                ),

            RightHand =
                controller.GetScoringLimbPosition(
                    PoseableLimbAction.RightArm
                ),

            LeftFoot =
                controller.GetScoringLimbPosition(
                    PoseableLimbAction.LeftLeg
                ),

            RightFoot =
                controller.GetScoringLimbPosition(
                    PoseableLimbAction.RightLeg
                ),

            SpineLeanDegrees =
                controller.GetScoringLeanDegrees(
                    PoseLeanChannel.Spine
                ),

            ChestLeanDegrees =
                controller.GetScoringLeanDegrees(
                    PoseLeanChannel.Chest
                ),

            HeadLeanDegrees =
                controller.GetScoringLeanDegrees(
                    PoseLeanChannel.Head
                )
        };

        return true;
    }
}

[System.Serializable]
public struct PoseCaptureSnapshot
{
    public Vector2 LeftHand;
    public Vector2 RightHand;
    public Vector2 LeftFoot;
    public Vector2 RightFoot;

    public float SpineLeanDegrees;
    public float ChestLeanDegrees;
    public float HeadLeanDegrees;
}
