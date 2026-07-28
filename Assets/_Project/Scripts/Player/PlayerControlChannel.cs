using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControlChannel : NetworkBehaviour
{
    public static readonly HashSet<PlayerControlChannel> ServerChannels = new();

    [Header("Input Actions")]
    [SerializeField]
    private InputActionReference moveAction;

    [SerializeField]
    private InputActionReference actionButton;

    [Header("Network Sending")]
    [SerializeField, Min(1f)]
    private float sendsPerSecond = 30f;

    private readonly SyncVar<PlayerControlRole> _role =
        new(PlayerControlRole.None);

    private float _nextSendTime;

    private Vector2 _lastSentAxis;
    private bool _lastSentAction;

    public PlayerControlRole Role => _role.Value;

    public Vector2 ServerAxisInput { get; private set; }

    public bool ServerActionHeld { get; private set; }

    public bool ServerActionPressedThisTick { get; private set; }

    public override void OnStartServer()
    {
        base.OnStartServer();

        ServerChannels.Add(this);

        AssignNextAvailableRole();
    }

    public override void OnStopServer()
    {
        ServerChannels.Remove(this);

        base.OnStopServer();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner)
            return;

        if (moveAction != null)
            moveAction.action.Enable();

        if (actionButton != null)
            actionButton.action.Enable();
    }

    public override void OnStopClient()
    {
        if (IsOwner)
        {
            if (moveAction != null)
                moveAction.action.Disable();

            if (actionButton != null)
                actionButton.action.Disable();
        }

        base.OnStopClient();
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        Vector2 axisInput = ReadAxisInput();
        bool actionHeld = ReadActionInput();

        bool axisChanged = axisInput != _lastSentAxis;
        bool actionChanged = actionHeld != _lastSentAction;
        bool sendIntervalReached =
            Time.unscaledTime >= _nextSendTime;

        if (!axisChanged &&
            !actionChanged &&
            !sendIntervalReached)
        {
            return;
        }

        _lastSentAxis = axisInput;
        _lastSentAction = actionHeld;

        _nextSendTime =
            Time.unscaledTime + (1f / sendsPerSecond);

        SubmitControlInputServerRpc(
            axisInput,
            actionHeld
        );
    }

    private Vector2 ReadAxisInput()
    {
        if (moveAction == null)
            return Vector2.zero;

        Vector2 input =
            moveAction.action.ReadValue<Vector2>();

        input = Vector2.ClampMagnitude(input, 1f);

        switch (Role)
        {
            case PlayerControlRole.ForwardBackward:
                return new Vector2(0f, input.y);

            case PlayerControlRole.Turning:
                return new Vector2(input.x, 0f);

            case PlayerControlRole.LeftArm:
            case PlayerControlRole.RightArm:
                return input;

            default:
                return Vector2.zero;
        }
    }

    private bool ReadActionInput()
    {
        if (Role != PlayerControlRole.RightArm)
            return false;

        if (actionButton == null)
            return false;

        return actionButton.action.IsPressed();
    }

    [ServerRpc]
    private void SubmitControlInputServerRpc(
        Vector2 axisInput,
        bool actionHeld)
    {
        axisInput = Vector2.ClampMagnitude(axisInput, 1f);

        ServerActionPressedThisTick =
            actionHeld && !ServerActionHeld;

        ServerAxisInput = SanitizeInputForRole(axisInput);
        ServerActionHeld = actionHeld;
    }

    private Vector2 SanitizeInputForRole(Vector2 input)
    {
        // The server validates input based on the assigned role.
        // A modified client cannot submit turning through the
        // ForwardBackward role, for example.

        input = Vector2.ClampMagnitude(input, 1f);

        switch (Role)
        {
            case PlayerControlRole.ForwardBackward:
                return new Vector2(
                    0f,
                    Mathf.Clamp(input.y, -1f, 1f)
                );

            case PlayerControlRole.Turning:
                return new Vector2(
                    Mathf.Clamp(input.x, -1f, 1f),
                    0f
                );

            case PlayerControlRole.LeftArm:
            case PlayerControlRole.RightArm:
                return input;

            default:
                return Vector2.zero;
        }
    }

    private void AssignNextAvailableRole()
    {
        if (!IsServerInitialized)
            return;

        PlayerControlRole[] roleOrder =
        {
            PlayerControlRole.ForwardBackward,
            PlayerControlRole.Turning,
            PlayerControlRole.LeftArm,
            PlayerControlRole.RightArm
        };

        foreach (PlayerControlRole possibleRole in roleOrder)
        {
            if (!IsRoleTaken(possibleRole))
            {
                _role.Value = possibleRole;

                Debug.Log(
                    $"Assigned {possibleRole} to client " +
                    $"{Owner.ClientId}.",
                    this
                );

                return;
            }
        }

        _role.Value = PlayerControlRole.None;

        Debug.LogWarning(
            $"No control role available for client " +
            $"{Owner.ClientId}.",
            this
        );
    }

    private bool IsRoleTaken(PlayerControlRole role)
    {
        foreach (PlayerControlChannel channel in ServerChannels)
        {
            if (channel == null || channel == this)
                continue;

            if (channel.Role == role)
                return true;
        }

        return false;
    }

    public void ClearTickFlags()
    {
        if (!IsServerInitialized)
            return;

        ServerActionPressedThisTick = false;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && IsOwner)
            SubmitControlInputServerRpc(Vector2.zero, false);
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused && IsOwner)
            SubmitControlInputServerRpc(Vector2.zero, false);
    }

    private void OnGUI()
    {
        if (!IsOwner)
            return;

        GUI.Label(
            new Rect(20f, 20f, 500f, 40f),
            $"Your role: {Role}"
        );
    }
}