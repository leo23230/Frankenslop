using System.Collections;
using UnityEngine;

public class RotateOnFailure : MonoBehaviour
{
    [SerializeField] private Transform objectToRotate;
    [SerializeField] private Vector3 failureRotationOffset = new Vector3(-90f, 0f, 0f);
    [SerializeField] private float rotationDuration = 0.75f;
    [SerializeField] private float resetRotationDuration = 0.5f;

    private Quaternion _startingRotation;
    private Coroutine _rotationRoutine;

    private void Awake()
    {
        if (objectToRotate == null) objectToRotate = transform;
        _startingRotation = objectToRotate.localRotation;
    }

    private void OnEnable()
    {
        MinigameStateManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        MinigameStateManager.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(MinigameState state)
    {
        switch (state)
        {
            case MinigameState.Failed:
                RotateToFailedPosition();
                break;

            case MinigameState.Resetting:
                ResetRotation();
                break;
        }
    }

    private void RotateToFailedPosition()
    {
        Quaternion targetRotation = _startingRotation * Quaternion.Euler(failureRotationOffset);
        StartRotation(targetRotation, rotationDuration);
    }

    private void ResetRotation()
    {
        StartRotation(_startingRotation, resetRotationDuration);
    }

    private void StartRotation(Quaternion targetRotation, float duration)
    {
        if (_rotationRoutine != null) StopCoroutine(_rotationRoutine);
        _rotationRoutine = StartCoroutine(RotateRoutine(targetRotation, duration));
    }

    private IEnumerator RotateRoutine(Quaternion targetRotation, float duration)
    {
        Quaternion startRotation = objectToRotate.localRotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            objectToRotate.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        objectToRotate.localRotation = targetRotation;
        _rotationRoutine = null;
    }
}