using System.Collections;
using TMPro;
using UnityEngine;

public class ScoreFlashUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private RectTransform animatedRoot;

    [SerializeField]
    private TMP_Text gradeText;

    [SerializeField]
    private TMP_Text scoreText;

    [Header("Timing")]
    [SerializeField, Min(0.01f)]
    private float appearDuration = 0.16f;

    [SerializeField, Min(0f)]
    private float holdDuration = 0.65f;

    [SerializeField, Min(0.01f)]
    private float disappearDuration = 0.20f;

    [Header("Animation")]
    [SerializeField]
    private Vector3 hiddenScale =
        new(0.55f, 0.55f, 1f);

    [SerializeField]
    private Vector3 visibleScale =
        Vector3.one;

    [SerializeField]
    private float upwardTravel = 45f;

    [SerializeField]
    private AnimationCurve appearCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );

    [SerializeField]
    private AnimationCurve disappearCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );

    [Header("Display Text")]
    [SerializeField]
    private string failText = "FAIL!";

    [SerializeField]
    private string passText = "PASS!";

    [SerializeField]
    private string goodText = "GREAT!";

    [SerializeField]
    private string perfectText = "PERFECT!";

    [Header("Grade Colors")]
    [SerializeField]
    private Color failColor =
        new(0.95f, 0.18f, 0.18f);

    [SerializeField]
    private Color passColor =
        new(0.95f, 0.78f, 0.16f);

    [SerializeField]
    private Color goodColor =
        new(0.20f, 0.82f, 0.40f);

    [SerializeField]
    private Color perfectColor =
        new(0.25f, 0.75f, 1f);

    private Coroutine _animationRoutine;
    private Vector2 _restingPosition;

    private void Awake()
    {
        canvasGroup =
            GetComponent<CanvasGroup>();

        animatedRoot =
            GetComponent<RectTransform>();

        if (canvasGroup == null)
        {
            Debug.LogError(
                "ScoreFlashUI requires a CanvasGroup on the same GameObject.",
                this
            );

            enabled = false;
            return;
        }

        if (animatedRoot == null)
        {
            Debug.LogError(
                "ScoreFlashUI requires a RectTransform.",
                this
            );

            enabled = false;
            return;
        }

        _restingPosition =
            animatedRoot.anchoredPosition;

        HideImmediately();

        Debug.Log(
            $"ScoreFlashUI initialized. Alpha is now {canvasGroup.alpha}.",
            this
        );
    }

    private void OnEnable()
    {
        MinigameResultEvents
            .OverallResultPresented +=
            HandleOverallResult;
    }

    private void OnDisable()
    {
        MinigameResultEvents
            .OverallResultPresented -=
            HandleOverallResult;

        if (_animationRoutine != null)
        {
            StopCoroutine(
                _animationRoutine
            );

            _animationRoutine = null;
        }
    }

    private void HandleOverallResult(
        MinigameScoreGrade grade,
        float normalizedScore)
    {
        if (_animationRoutine != null)
        {
            StopCoroutine(
                _animationRoutine
            );
        }

        ApplyContent(
            grade,
            normalizedScore
        );

        _animationRoutine =
            StartCoroutine(
                PlayFlashRoutine()
            );
    }

    private void ApplyContent(
        MinigameScoreGrade grade,
        float normalizedScore)
    {
        if (gradeText != null)
        {
            gradeText.text =
                GetDisplayText(
                    grade
                );

            gradeText.color =
                GetDisplayColor(
                    grade
                );
        }

        if (scoreText != null)
        {
            scoreText.text =
                Mathf.Clamp01(
                    normalizedScore
                ).ToString("P0");
        }
    }

    private IEnumerator PlayFlashRoutine()
    {
        if (canvasGroup == null ||
            animatedRoot == null)
        {
            yield break;
        }

        canvasGroup.gameObject.SetActive(
            true
        );

        canvasGroup.alpha = 0f;

        animatedRoot.localScale =
            hiddenScale;

        animatedRoot.anchoredPosition =
            _restingPosition -
            Vector2.up *
            upwardTravel *
            0.35f;

        float elapsed = 0f;

        while (elapsed < appearDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float rawProgress =
                Mathf.Clamp01(
                    elapsed /
                    appearDuration
                );

            float progress =
                appearCurve.Evaluate(
                    rawProgress
                );

            canvasGroup.alpha =
                progress;

            animatedRoot.localScale =
                Vector3.LerpUnclamped(
                    hiddenScale,
                    visibleScale,
                    progress
                );

            animatedRoot.anchoredPosition =
                Vector2.LerpUnclamped(
                    _restingPosition -
                    Vector2.up *
                    upwardTravel *
                    0.35f,
                    _restingPosition,
                    progress
                );

            yield return null;
        }

        canvasGroup.alpha = 1f;
        animatedRoot.localScale =
            visibleScale;

        animatedRoot.anchoredPosition =
            _restingPosition;

        if (holdDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                holdDuration
            );
        }

        Vector2 disappearTarget =
            _restingPosition +
            Vector2.up *
            upwardTravel;

        elapsed = 0f;

        while (elapsed < disappearDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float rawProgress =
                Mathf.Clamp01(
                    elapsed /
                    disappearDuration
                );

            float progress =
                disappearCurve.Evaluate(
                    rawProgress
                );

            canvasGroup.alpha =
                1f -
                progress;

            animatedRoot.localScale =
                Vector3.LerpUnclamped(
                    visibleScale,
                    visibleScale *
                    1.08f,
                    progress
                );

            animatedRoot.anchoredPosition =
                Vector2.LerpUnclamped(
                    _restingPosition,
                    disappearTarget,
                    progress
                );

            yield return null;
        }

        HideImmediately();

        _animationRoutine = null;
    }

    private void HideImmediately()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        animatedRoot.localScale =
            hiddenScale;

        animatedRoot.anchoredPosition =
            _restingPosition;
    }

    private string GetDisplayText(
        MinigameScoreGrade grade)
    {
        return grade switch
        {
            MinigameScoreGrade.Fail =>
                failText,

            MinigameScoreGrade.Pass =>
                passText,

            MinigameScoreGrade.Good =>
                goodText,

            MinigameScoreGrade.Perfect =>
                perfectText,

            _ =>
                string.Empty
        };
    }

    private Color GetDisplayColor(
        MinigameScoreGrade grade)
    {
        return grade switch
        {
            MinigameScoreGrade.Fail =>
                failColor,

            MinigameScoreGrade.Pass =>
                passColor,

            MinigameScoreGrade.Good =>
                goodColor,

            MinigameScoreGrade.Perfect =>
                perfectColor,

            _ =>
                Color.white
        };
    }
}
