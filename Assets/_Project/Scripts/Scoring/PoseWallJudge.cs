using FishNet.Object;
using UnityEngine;

public class PoseWallJudge : NetworkBehaviour
{
    [Header("Evaluation")]
    [SerializeField]
    private PoseTemplate poseTemplate;

    [SerializeField]
    private PoseableLimbIKController poseController;

    [SerializeField]
    private bool evaluateOnlyOnce = true;

    private bool _hasEvaluated;

    public PoseEvaluationResult LatestResult {
        get;
        private set;
    }

    [Server]
    public PoseEvaluationResult EvaluateNow()
    {
        if (evaluateOnlyOnce &&
            _hasEvaluated)
        {
            return LatestResult;
        }

        if (poseTemplate == null ||
            poseController == null)
        {
            Debug.LogError(
                "PoseWallJudge is missing its template or controller.",
                this
            );

            return default;
        }

        LatestResult =
            PoseEvaluator.Evaluate(
                poseTemplate,
                poseController
            );

        _hasEvaluated = true;

        ShowOverallResultObserversRpc(
            LatestResult.OverallGrade,
            LatestResult.OverallNormalizedScore
        );

        /*
         * Individual player scores remain server-side in LatestResult.
         * Do not include them in the ObserversRpc unless you intentionally
         * want clients to receive them.
         */

        Debug.Log(
            $"Pose result: {LatestResult.OverallGrade} " +
            $"({LatestResult.OverallNormalizedScore:P1}).",
            this
        );

        return LatestResult;
    }

    [Server]
    public void ResetEvaluation()
    {
        _hasEvaluated = false;
        LatestResult = default;
    }

    [ObserversRpc]
    private void ShowOverallResultObserversRpc(
    MinigameScoreGrade grade,
    float normalizedScore)
    {
        MinigameResultEvents.RaiseOverallResult(
            grade,
            normalizedScore
        );

        Debug.Log(
            $"Team pose: {grade} ({normalizedScore:P1})",
            this
        );
    }
}
