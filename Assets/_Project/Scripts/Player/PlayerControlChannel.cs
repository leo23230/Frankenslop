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

    /*
     * Stable player identity.
     *
     * Body controllers should use Slot to determine which player
     * supplied an input.
     */
    private readonly SyncVar<PlayerSlot> _slot =
        new(PlayerSlot.None);

    /*
     * Legacy walking-body assignment.
     *
     * This remains here temporarily so your existing walking prefab
     * does not need to be rewritten immediately.
     */
    private readonly SyncVar<PlayerControlRole> _role =
        new(PlayerControlRole.None);

    private float _nextSendTime;

    private Vector2 _lastSentAxis;
    private bool _lastSentAction;

    public PlayerSlot Slot => _slot.Value;

    public PlayerControlRole Role => _role.Value;

    /*
     * Full server-validated Vector2 input.
     *
     * New body controllers should use this property.
     */
    public Vector2 RawServerAxisInput { get; private set; }

    public bool RawServerActionHeld { get; private set; }

    public bool RawServerActionPressedThisTick { get; private set; }

    /*
     * Input filtered according to the old PlayerControlRole.
     *
     * Existing walking-body scripts can continue using these.
     */
    public Vector2 ServerAxisInput { get; private set; }

    public bool ServerActionHeld { get; private set; }

    public bool ServerActionPressedThisTick { get; private set; }

    public override void OnStartServer()
    {
        base.OnStartServer();

        ServerChannels.Add(this);

        AssignNextAvailableSlot();
        AssignNextAvailableLegacyRole();
    }

    public override void OnStopServer()
    {
        ServerChannels.Remove(this);

        RawServerAxisInput = Vector2.zero;
        RawServerActionHeld = false;
        RawServerActionPressedThisTick = false;

        ServerAxisInput = Vector2.zero;
        ServerActionHeld = false;
        ServerActionPressedThisTick = false;

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

        Vector2 axisInput = ReadRawAxisInput();
        bool actionHeld = ReadRawActionInput();

        bool axisChanged =
            axisInput != _lastSentAxis;

        bool actionChanged =
            actionHeld != _lastSentAction;

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
            Time.unscaledTime +
            (1f / sendsPerSecond);

        SubmitControlInputServerRpc(
            axisInput,
            actionHeld
        );
    }

    private Vector2 ReadRawAxisInput()
    {
        if (moveAction == null)
            return Vector2.zero;

        Vector2 input =
            moveAction.action.ReadValue<Vector2>();

        return Vector2.ClampMagnitude(input, 1f);
    }

    private bool ReadRawActionInput()
    {
        if (actionButton == null)
            return false;

        return actionButton.action.IsPressed();
    }

    [ServerRpc]
    private void SubmitControlInputServerRpc(
        Vector2 axisInput,
        bool actionHeld)
    {
        Vector2 sanitizedRawInput =
            Vector2.ClampMagnitude(axisInput, 1f);

        /*
         * New body-system values.
         *
         * These retain both X and Y regardless of the old role.
         */
        RawServerActionPressedThisTick =
            actionHeld &&
            !RawServerActionHeld;

        RawServerAxisInput =
            sanitizedRawInput;

        RawServerActionHeld =
            actionHeld;

        /*
         * Legacy walking-body values.
         *
         * These behave like your previous implementation.
         */
        ServerAxisInput =
            SanitizeInputForLegacyRole(
                sanitizedRawInput
            );

        bool legacyActionHeld =
            Role == PlayerControlRole.RightArm &&
            actionHeld;

        ServerActionPressedThisTick =
            legacyActionHeld &&
            !ServerActionHeld;

        ServerActionHeld =
            legacyActionHeld;
    }

    private Vector2 SanitizeInputForLegacyRole(
        Vector2 input)
    {
        input =
            Vector2.ClampMagnitude(input, 1f);

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

    private void AssignNextAvailableSlot()
    {
        if (!IsServerInitialized)
            return;

        PlayerSlot[] slotOrder =
        {
            PlayerSlot.Player1,
            PlayerSlot.Player2,
            PlayerSlot.Player3,
            PlayerSlot.Player4
        };

        foreach (PlayerSlot possibleSlot in slotOrder)
        {
            if (IsSlotTaken(possibleSlot))
                continue;

            _slot.Value = possibleSlot;

            Debug.Log(
                $"Assigned {possibleSlot} to client " +
                $"{Owner.ClientId}.",
                this
            );

            return;
        }

        _slot.Value = PlayerSlot.None;

        Debug.LogWarning(
            $"No player slot available for client " +
            $"{Owner.ClientId}.",
            this
        );
    }

    private bool IsSlotTaken(PlayerSlot slot)
    {
        foreach (PlayerControlChannel channel
                 in ServerChannels)
        {
            if (channel == null ||
                channel == this)
            {
                continue;
            }

            if (channel.Slot == slot)
                return true;
        }

        return false;
    }

    private void AssignNextAvailableLegacyRole()
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

        foreach (PlayerControlRole possibleRole
                 in roleOrder)
        {
            if (IsLegacyRoleTaken(possibleRole))
                continue;

            _role.Value = possibleRole;

            Debug.Log(
                $"Assigned legacy role {possibleRole} " +
                $"to client {Owner.ClientId}.",
                this
            );

            return;
        }

        _role.Value = PlayerControlRole.None;

        Debug.LogWarning(
            $"No legacy control role available for client " +
            $"{Owner.ClientId}.",
            this
        );
    }

    private bool IsLegacyRoleTaken(
        PlayerControlRole role)
    {
        foreach (PlayerControlChannel channel
                 in ServerChannels)
        {
            if (channel == null ||
                channel == this)
            {
                continue;
            }

            if (channel.Role == role)
                return true;
        }

        return false;
    }

    public void ClearTickFlags()
    {
        if (!IsServerInitialized)
            return;

        RawServerActionPressedThisTick = false;
        ServerActionPressedThisTick = false;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus &&
            IsOwner &&
            IsClientInitialized)
        {
            SubmitControlInputServerRpc(
                Vector2.zero,
                false
            );
        }
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused &&
            IsOwner &&
            IsClientInitialized)
        {
            SubmitControlInputServerRpc(
                Vector2.zero,
                false
            );
        }
    }

    private void OnGUI()
    {
        if (!IsOwner)
            return;

        GUI.Label(
            new Rect(20f, 20f, 500f, 25f),
            $"Player slot: {Slot}"
        );

        GUI.Label(
            new Rect(20f, 45f, 500f, 25f),
            $"Legacy walking role: {Role}"
        );
    }
}