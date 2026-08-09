using UnityEngine;

public class HoleInTheWallEffects : MonoBehaviour
{
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
        if (CameraEffects.Instance == null) return;

        switch (state)
        {
            case MinigameState.Failed:
                CameraEffects.Instance.ZoomBackOnFail();
                CameraEffects.Instance.Shake(0.3f, 0.5f);
                break;

            case MinigameState.Resetting:
                CameraEffects.Instance.ResetZoom();
                break;
        }
    }
}