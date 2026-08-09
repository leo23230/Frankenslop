using System;
using System.Collections;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class MinigameStateManager : NetworkBehaviour
{

    public static event Action<MinigameState> OnStateChanged;

    [Header("Players")]
    [SerializeField]
    private PoseableLimbIKController[] players;

    [Header("Walls")]
    [SerializeField]
    private ServerPoseWall[] walls;

    [Header("Failure")]
    [SerializeField, Min(0f)]
    private float failureResetDelay = 3f;

    private readonly SyncVar<MinigameState> _state = new(MinigameState.Playing);

    private Coroutine _failureRoutine;

    public MinigameState State => _state.Value;

    public bool IsPlaying => _state.Value == MinigameState.Playing;

    public override void OnStartServer()
    {
        base.OnStartServer();

        ServerPoseWall.ServerWallFailed +=HandleWallFailed;

        SetState(MinigameState.Playing);
    }

    public override void OnStopServer()
    {
        ServerPoseWall.ServerWallFailed -= HandleWallFailed;

        if (_failureRoutine != null)
        {
            StopCoroutine(_failureRoutine);
            _failureRoutine = null;
        }

        base.OnStopServer();
    }

    [Server]
    private void SetState(MinigameState newState)
    {
        if (_state.Value == newState)
            return;

        _state.Value = newState;

        StateChangedObserversRpc(newState);
    }
    [ObserversRpc]
    private void StateChangedObserversRpc(
    MinigameState newState)
    {
        OnStateChanged?.Invoke(newState);
    }

    [Server]
    private void HandleWallFailed(ServerPoseWall failedWall){
        if (_state.Value != MinigameState.Playing)
        {
            return;
        }

        Debug.Log(
            $"Minigame failed on {failedWall.name}.",
            this
        );

        _failureRoutine =StartCoroutine(FailureRoutine());
    }

    [Server]
    private IEnumerator FailureRoutine()
    {
        SetState(MinigameState.Failed);

        StopWalls();
        KillPlayers();
        //CameraEffects.Instance.ZoomBackOnFail();
        //CameraEffects.Instance.Shake(0.3f, 0.5f);

        yield return new WaitForSeconds(failureResetDelay);

        SetState(MinigameState.Resetting);

        ResetMinigameInternal();

        SetState(MinigameState.Playing);

        _failureRoutine = null;
    }

    [Server]
    private void KillPlayers()
    {
        if (players == null)
            return;

        foreach (PoseableLimbIKController player in players)
        {
            if (player == null)
                continue;

            player.ServerDie();
        }
    }

    [Server]
    private void StopWalls()
    {
        if (walls == null) return;

        foreach (ServerPoseWall wall in walls)
        {
            if (wall == null)
                continue;

            wall.SetMovementEnabled(false);
        }
    }

    [Server]
    private void ResetPlayers()
    {
        if (players == null)
            return;

        foreach (PoseableLimbIKController player in players)
        {
            if (player == null)
                continue;

            player.ResetBody();
        }
    }

    [Server]
    private void ResetWalls()
    {
        if (walls == null)
            return;

        foreach (ServerPoseWall wall in walls)
        {
            if (wall == null)
                continue;

            wall.ResetWall();
        }
    }

    [Server]
    private void ResetMinigameInternal()
    {
        //CameraEffects.Instance.ResetZoom();
        ResetPlayers();
        ResetWalls();

        Debug.Log(
            "Minigame reset complete.",
            this
        );
    }

    // ------------------------------------------------
    // MANUAL RESTART
    // ------------------------------------------------

    public void RequestRestart()
    {
        if (IsServerStarted)
        {
            ServerRestart();
            return;
        }

        RequestRestartServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestRestartServerRpc()
    {
        ServerRestart();
    }

    [Server]
    public void ServerRestart()
    {
        if (_failureRoutine != null)
        {
            StopCoroutine(_failureRoutine);
            _failureRoutine = null;
        }

        SetState(MinigameState.Resetting);

        ResetMinigameInternal();

        SetState(MinigameState.Playing);
    }
}