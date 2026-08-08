using System;
using UnityEngine;

[Serializable]
public struct PosePoint
{
    [SerializeField] private Vector2 position;

    [Tooltip("Elliptical X/Y tolerance. A point at the ellipse edge scores zero.")]
    [SerializeField] private Vector2 tolerance;

    [SerializeField, Min(0f)] private float weight;

    [Tooltip("When enabled, this component must reach Minimum Required Score.")]
    [SerializeField] private bool required;

    [Range(0f, 1f)]
    [SerializeField] private float minimumRequiredScore;

    public Vector2 Position => position;
    public Vector2 Tolerance => tolerance;
    public float Weight => weight;
    public bool Required => required;
    public float MinimumRequiredScore => minimumRequiredScore;

    public void SetCapturedPosition(Vector2 value)
    {
        position = value;
    }

    public static PosePoint Create(
        Vector2 capturedPosition,
        Vector2 pointTolerance,
        float pointWeight,
        bool isRequired = false,
        float requiredMinimum = 0f)
    {
        return new PosePoint
        {
            position = capturedPosition,
            tolerance = new Vector2(
                Mathf.Max(0.001f, pointTolerance.x),
                Mathf.Max(0.001f, pointTolerance.y)
            ),
            weight = Mathf.Max(0f, pointWeight),
            required = isRequired,
            minimumRequiredScore = Mathf.Clamp01(requiredMinimum)
        };
    }
}
