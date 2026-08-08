using System.Collections;
using UnityEngine;

public class CrowdHopper : MonoBehaviour
{
    [Header("Normal Hop")]
    [SerializeField, Min(0f)] private float maxHeight = 1f;
    [SerializeField, Min(0.01f)] private float hopDuration = 0.5f;
    [SerializeField, Min(0f)] private float intervalBetweenHops = 1f;

    [Header("Failure Reaction")]
    [SerializeField, Min(0)] private int failureHopCount = 3;
    [SerializeField, Min(0f)] private float failureHopHeight = 0.25f;
    [SerializeField, Min(0.01f)] private float failureHopDuration = 0.9f;
    [SerializeField, Min(0f)] private float failureHopInterval = 0.5f;
    [SerializeField, Min(0f)] private float failureReactionDelay = 0.15f;

    private Vector3 _startingPosition;
    private Coroutine _hopRoutine;
    private Coroutine _failureRoutine;

    private void Awake()
    {
        _startingPosition = transform.position;
    }

    private void OnEnable()
    {
        MinigameStateManager.OnStateChanged += HandleStateChanged;
        StartNormalHopping();
    }

    private void OnDisable()
    {
        MinigameStateManager.OnStateChanged -= HandleStateChanged;
        StopAllHopping();
        transform.position = _startingPosition;
    }

    private void HandleStateChanged(MinigameState state)
    {
        switch (state)
        {
            case MinigameState.Playing:
                StartNormalHopping();
                break;
            case MinigameState.Failed:
                StartFailureReaction();
                break;
            case MinigameState.Resetting:
                StopAllHopping();
                transform.position = _startingPosition;
                break;
        }
    }

    private void StartNormalHopping()
    {
        StopAllHopping();
        _hopRoutine = StartCoroutine(NormalHopLoop());
    }

    private IEnumerator NormalHopLoop()
    {
        while (true)
        {
            yield return HopOnce(maxHeight, hopDuration);

            if (intervalBetweenHops > 0f)
                yield return new WaitForSeconds(intervalBetweenHops);
        }
    }

    private void StartFailureReaction()
    {
        StopAllHopping();
        transform.position = _startingPosition;
        _failureRoutine = StartCoroutine(FailureHopRoutine());
    }

    private IEnumerator FailureHopRoutine()
    {
        if (failureReactionDelay > 0f)
            yield return new WaitForSeconds(failureReactionDelay);

        for (int i = 0; i < failureHopCount; i++)
        {
            float hopScale = 1f - ((float)i / Mathf.Max(1, failureHopCount)) * 0.65f;
            float currentHeight = failureHopHeight * hopScale;
            float currentDuration = failureHopDuration * (1f + i * 0.35f);

            yield return HopOnce(currentHeight, currentDuration);

            if (i < failureHopCount - 1 && failureHopInterval > 0f)
                yield return new WaitForSeconds(failureHopInterval);
        }

        transform.position = _startingPosition;
        _failureRoutine = null;
    }

    private IEnumerator HopOnce(float height, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float heightProgress = Mathf.Sin(progress * Mathf.PI);
            transform.position = _startingPosition + Vector3.up * height * heightProgress;
            yield return null;
        }

        transform.position = _startingPosition;
    }

    private void StopAllHopping()
    {
        if (_hopRoutine != null)
        {
            StopCoroutine(_hopRoutine);
            _hopRoutine = null;
        }

        if (_failureRoutine != null)
        {
            StopCoroutine(_failureRoutine);
            _failureRoutine = null;
        }
    }
}