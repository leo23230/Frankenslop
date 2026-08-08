using System;
using UnityEngine;

[Serializable]
public struct PoseLeanPoint
{
    [Tooltip("Signed side-lean angle in degrees relative to the neutral pose.")]
    [SerializeField] private float targetDegrees;

    [Tooltip("Angular error in degrees at which this component scores zero.")]
    [SerializeField, Min(0.01f)] private float toleranceDegrees;

    [SerializeField, Min(0f)] private float weight;

    [Tooltip("When enabled, this component must reach Minimum Required Score.")]
    [SerializeField] private bool required;

    [Range(0f, 1f)]
    [SerializeField] private float minimumRequiredScore;

    public float TargetDegrees => targetDegrees;
    public float ToleranceDegrees => toleranceDegrees;
    public float Weight => weight;
    public bool Required => required;
    public float MinimumRequiredScore => minimumRequiredScore;

    public void SetCapturedDegrees(float value)
    {
        targetDegrees = value;
    }

    public static PoseLeanPoint Create(
        float capturedDegrees,
        float angularTolerance,
        float pointWeight,
        bool isRequired = false,
        float requiredMinimum = 0f)
    {
        return new PoseLeanPoint
        {
            targetDegrees = capturedDegrees,
            toleranceDegrees = Mathf.Max(0.01f, angularTolerance),
            weight = Mathf.Max(0f, pointWeight),
            required = isRequired,
            minimumRequiredScore = Mathf.Clamp01(requiredMinimum)
        };
    }
}
