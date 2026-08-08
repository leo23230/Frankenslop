using System.Collections;
using UnityEngine;

public class CameraEffects : MonoBehaviour
{
    public static CameraEffects Instance { get; private set; }

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    [Header("Zoom / Position")]
    [SerializeField] private Vector3 normalLocalPosition;
    [SerializeField] private Vector3 failLocalPosition;
    [SerializeField] private float moveDuration = 0.5f;

    [Header("Shake")]
    [SerializeField] private float defaultShakeDuration = 0.3f;
    [SerializeField] private float defaultShakeStrength = 0.15f;

    private Coroutine moveCoroutine;
    private Coroutine shakeCoroutine;

    private Vector3 shakeOffset;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        normalLocalPosition = cameraTransform.localPosition;
    }

    public void ZoomBackOnFail()
    {
        MoveTo(failLocalPosition);
    }

    public void ResetZoom()
    {
        MoveTo(normalLocalPosition);
    }

    private void MoveTo(Vector3 targetPosition)
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(MoveCameraTo(targetPosition));
    }

    private IEnumerator MoveCameraTo(Vector3 targetPosition)
    {
        Vector3 startPosition = cameraTransform.localPosition - shakeOffset;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / moveDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            Vector3 position = Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );

            cameraTransform.localPosition = position + shakeOffset;

            yield return null;
        }

        cameraTransform.localPosition = targetPosition + shakeOffset;

        moveCoroutine = null;
    }

    // Uses inspector defaults.
    public void Shake()
    {
        Shake(defaultShakeDuration, defaultShakeStrength);
    }

    // Lets you specify strength/duration per effect.
    public void Shake(float duration, float strength)
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        shakeCoroutine = StartCoroutine(
            ShakeCoroutine(duration, strength)
        );
    }

    private IEnumerator ShakeCoroutine(float duration, float strength)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Remove previous frame's shake.
            cameraTransform.localPosition -= shakeOffset;

            shakeOffset = Random.insideUnitSphere * strength;

            // Usually don't want much Z shake on a gameplay camera.
            shakeOffset.z *= 0.25f;

            cameraTransform.localPosition += shakeOffset;

            yield return null;
        }

        // Remove final shake offset.
        cameraTransform.localPosition -= shakeOffset;
        shakeOffset = Vector3.zero;

        shakeCoroutine = null;
    }
}
