using UnityEngine;

public class LivePoseScoreDebugger : MonoBehaviour
{
    [Header("Evaluation")]
    [SerializeField]
    private PoseTemplate poseTemplate;

    [SerializeField]
    private PoseableLimbIKController poseController;

    [Header("Live Debugging")]
    [SerializeField]
    private bool evaluateContinuously = true;

    [SerializeField, Min(1f)]
    private float evaluationsPerSecond = 10f;

    [SerializeField]
    private bool showDebugUI = true;

    [SerializeField]
    private bool logEveryEvaluation;

    [SerializeField]
    private bool logGradeChanges = true;

    [Header("UI Position")]
    [SerializeField]
    private Vector2 screenPosition =
        new(20f, 140f);

    private float _nextEvaluationTime;

    private PoseEvaluationResult _latestResult;
    private MinigameScoreGrade _previousGrade;

    private bool _hasResult;
    private string _statusMessage =
        "Waiting for first evaluation...";

    public PoseEvaluationResult LatestResult =>
        _latestResult;

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        if (!evaluateContinuously)
            return;

        if (Time.unscaledTime <
            _nextEvaluationTime)
        {
            return;
        }

        float interval =
            1f /
            Mathf.Max(
                1f,
                evaluationsPerSecond
            );

        _nextEvaluationTime =
            Time.unscaledTime +
            interval;

        EvaluatePreview();

#endif
    }

    public void EvaluatePreview()
    {
        if (poseTemplate == null)
        {
            _statusMessage =
                "Missing PoseTemplate.";

            return;
        }

        if (poseController == null)
        {
            _statusMessage =
                "Missing PoseableLimbIKController.";

            return;
        }

        if (!poseController.HasScoringState)
        {
            _statusMessage =
                "Waiting for pose scoring state...";

            return;
        }

        PoseEvaluationResult newResult =
            PoseEvaluator.Evaluate(
                poseTemplate,
                poseController
            );

        bool gradeChanged =
            !_hasResult ||
            newResult.OverallGrade !=
            _previousGrade;

        _latestResult =
            newResult;

        _previousGrade =
            newResult.OverallGrade;

        _hasResult = true;

        _statusMessage =
            $"Evaluating at " +
            $"{evaluationsPerSecond:0.#} times/second";

        if (logEveryEvaluation)
        {
            Debug.Log(
                $"Live pose: " +
                $"{newResult.OverallGrade} — " +
                $"{newResult.OverallNormalizedScore:P1}",
                this
            );
        }
        else if (logGradeChanges &&
                 gradeChanged)
        {
            Debug.Log(
                $"Live pose grade changed to " +
                $"{newResult.OverallGrade} — " +
                $"{newResult.OverallNormalizedScore:P1}",
                this
            );
        }
    }

    private void OnGUI()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        if (!showDebugUI)
            return;

        float left =
            screenPosition.x;

        float top =
            screenPosition.y;

        const float width = 430f;
        const float lineHeight = 22f;
        const float padding = 12f;

        int visibleLines =
            _hasResult
                ? 13
                : 3;

        float height =
            padding * 2f +
            visibleLines *
            lineHeight;

        GUI.Box(
            new Rect(
                left,
                top,
                width,
                height
            ),
            GUIContent.none
        );

        float textLeft =
            left +
            padding;

        float textTop =
            top +
            padding;

        DrawLabel(
            textLeft,
            textTop,
            width,
            lineHeight,
            0,
            "Live Pose Evaluation"
        );

        DrawLabel(
            textLeft,
            textTop,
            width,
            lineHeight,
            1,
            _statusMessage
        );

        if (!_hasResult)
        {
            DrawLabel(
                textLeft,
                textTop,
                width,
                lineHeight,
                2,
                "Check the assigned template and controller."
            );

            return;
        }

        DrawLabel(
            textLeft,
            textTop,
            width,
            lineHeight,
            2,
            $"Overall: " +
            $"{_latestResult.OverallGrade} " +
            $"({_latestResult.OverallNormalizedScore:P1})"
        );

        DrawLabel(
            textLeft,
            textTop,
            width,
            lineHeight,
            3,
            $"Required components: " +
            $"{(_latestResult.RequiredComponentsPassed ? "Passed" : "Failed")}"
        );

        DrawLabel(
            textLeft,
            textTop,
            width,
            lineHeight,
            5,
            $"Left Hand:  " +
            $"{_latestResult.LeftHandScore:P1}"
        );

        DrawLabel(
            textLeft,
            textTop,
            width,
            lineHeight,
            6,
            $"Right Hand: " +
            $"{_latestResult.RightHandScore:P1}"
        );

        DrawLabel(
            textLeft,
            textTop,
            width,
            lineHeight,
            7,
            $"Left Foot:  " +
            $"{_latestResult.LeftFootScore:P1}"
        );

        DrawLabel(
            textLeft,
            textTop,
            width,
            lineHeight,
            8,
            $"Right Foot: " +
            $"{_latestResult.RightFootScore:P1}"
        );

        DrawLabel(
            textLeft,
            textTop,
            width,
            lineHeight,
            10,
            $"Spine Lean: " +
            $"{_latestResult.SpineLeanScore:P1}"
        );

        DrawLabel(
            textLeft,
            textTop,
            width,
            lineHeight,
            11,
            $"Chest Lean: " +
            $"{_latestResult.ChestLeanScore:P1}"
        );

        DrawLabel(
            textLeft,
            textTop,
            width,
            lineHeight,
            12,
            $"Head Lean:  " +
            $"{_latestResult.HeadLeanScore:P1}"
        );

#endif
    }

    private static void DrawLabel(
        float left,
        float top,
        float width,
        float lineHeight,
        int line,
        string text)
    {
        GUI.Label(
            new Rect(
                left,
                top +
                line *
                lineHeight,
                width,
                lineHeight
            ),
            text
        );
    }
}