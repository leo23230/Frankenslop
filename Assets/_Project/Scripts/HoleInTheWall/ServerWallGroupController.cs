using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class ServerWallGroupController : NetworkBehaviour
{
    [Header("Walls")]
    [SerializeField]
    private List<ServerPoseWall> walls =
        new();

    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float movementSpeed = 2f;

    [SerializeField]
    private bool startAutomatically = true;

    private bool _isMoving;

    public bool IsMoving => _isMoving;

    public float MovementSpeed =>
        movementSpeed;

    public override void OnStartServer()
    {
        base.OnStartServer();

        TimeManager.OnTick +=
            HandleServerTick;

        if (startAutomatically)
        {
            StartMoving();
        }
    }

    public override void OnStopServer()
    {
        TimeManager.OnTick -=
            HandleServerTick;

        base.OnStopServer();
    }

    private void HandleServerTick()
    {
        if (!_isMoving)
            return;

        float deltaTime =
            (float)TimeManager.TickDelta;

        float movementDistance =
            movementSpeed *
            deltaTime;

        for (int i = walls.Count - 1;
             i >= 0;
             i--)
        {
            ServerPoseWall wall =
                walls[i];

            if (wall == null)
            {
                walls.RemoveAt(i);
                continue;
            }

            wall.ServerMove(
                movementDistance
            );
        }
    }

    [Server]
    public void StartMoving()
    {
        _isMoving = true;
    }

    [Server]
    public void StopMoving()
    {
        _isMoving = false;
    }

    [Server]
    public void SetMovementSpeed(
        float newSpeed)
    {
        movementSpeed =
            Mathf.Max(
                0f,
                newSpeed
            );
    }

    [Server]
    public void AddWall(
        ServerPoseWall wall)
    {
        if (wall == null ||
            walls.Contains(wall))
        {
            return;
        }

        walls.Add(wall);
    }

    [Server]
    public void RemoveWall(
        ServerPoseWall wall)
    {
        if (wall == null)
            return;

        walls.Remove(wall);
    }

    [Server]
    public void ResetAllWalls()
    {
        _isMoving = false;

        foreach (ServerPoseWall wall
                 in walls)
        {
            if (wall == null)
                continue;

            wall.ResetWall();
        }
    }

#if UNITY_EDITOR

    [ContextMenu("Find Walls In Children")]
    private void FindWallsInChildren()
    {
        walls.Clear();

        walls.AddRange(
            GetComponentsInChildren<
                ServerPoseWall
            >(
                true
            )
        );

        UnityEditor.EditorUtility.SetDirty(
            this
        );
    }

#endif
}