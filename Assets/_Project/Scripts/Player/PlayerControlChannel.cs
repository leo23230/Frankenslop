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

    [Header("Debug Slot Testing")]
    [Tooltip(
        "Allows the owning client to switch PlayerSlot while running " +
        "in the Editor or a Development Build."
    )]
    [SerializeField]
    private bool enableSingleClientSlotSwitching = true;

    [SerializeField]
    private bool showDebugSlotUI = true;

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

        ClearServerInputState();

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        HandleDebugSlotSwitching();
#endif

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD

    private void HandleDebugSlotSwitching()
    {
        if (!enableSingleClientSlotSwitching)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        PlayerSlot requestedSlot = PlayerSlot.None;

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            requestedSlot = PlayerSlot.Player1;
        }
        else if (keyboard.digit2Key.wasPressedThisFrame)
        {
            requestedSlot = PlayerSlot.Player2;
        }
        else if (keyboard.digit3Key.wasPressedThisFrame)
        {
            requestedSlot = PlayerSlot.Player3;
        }
        else if (keyboard.digit4Key.wasPressedThisFrame)
        {
            requestedSlot = PlayerSlot.Player4;
        }
        else if (keyboard.tabKey.wasPressedThisFrame)
        {
            requestedSlot = GetNextDebugSlot(Slot);
        }

        if (requestedSlot == PlayerSlot.None ||
            requestedSlot == Slot)
        {
            return;
        }

        /*
         * Clear the locally cached sent state so a zeroed input packet
         * is sent immediately after changing slots.
         */
        _lastSentAxis = Vector2.zero;
        _lastSentAction = false;
        _nextSendTime = 0f;

        RequestDebugSlotServerRpc(requestedSlot);
    }

    private static PlayerSlot GetNextDebugSlot(
        PlayerSlot currentSlot)
    {
        return currentSlot switch
        {
            PlayerSlot.Player1 => PlayerSlot.Player2,
            PlayerSlot.Player2 => PlayerSlot.Player3,
            PlayerSlot.Player3 => PlayerSlot.Player4,
            PlayerSlot.Player4 => PlayerSlot.Player1,
            _ => PlayerSlot.Player1
        };
    }

    [ServerRpc]
    private void RequestDebugSlotServerRpc(
        PlayerSlot requestedSlot)
    {
        if (!enableSingleClientSlotSwitching)
            return;

        if (!IsValidPlayableSlot(requestedSlot))
            return;

        if (requestedSlot == _slot.Value)
            return;

        if (IsSlotTaken(requestedSlot))
        {
            Debug.LogWarning(
                $"Cannot switch client {Owner.ClientId} to " +
                $"{requestedSlot}: that slot is already occupied.",
                this
            );

            return;
        }

        PlayerSlot previousSlot = _slot.Value;

        /*
         * Remove all server-side input from the old assignment before
         * changing slots. This prevents the previously controlled limb
         * from receiving stale input for another server tick.
         */
        ClearServerInputState();

        _slot.Value = requestedSlot;

        Debug.Log(
            $"Debug slot changed from {previousSlot} to " +
            $"{requestedSlot} for client {Owner.ClientId}.",
            this
        );
    }

    private static bool IsValidPlayableSlot(
        PlayerSlot slot)
    {
        return slot == PlayerSlot.Player1 ||
               slot == PlayerSlot.Player2 ||
               slot == PlayerSlot.Player3 ||
               slot == PlayerSlot.Player4;
    }

#endif

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

    private void ClearServerInputState()
    {
        RawServerAxisInput = Vector2.zero;
        RawServerActionHeld = false;
        RawServerActionPressedThisTick = false;

        ServerAxisInput = Vector2.zero;
        ServerActionHeld = false;
        ServerActionPressedThisTick = false;
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD

        if (enableSingleClientSlotSwitching &&
            showDebugSlotUI)
        {
            GUI.Label(
                new Rect(20f, 70f, 650f, 25f),
                "Testing: press 1-4 to select a slot, or Tab to cycle."
            );

            GUI.Label(
                new Rect(20f, 95f, 650f, 25f),
                GetDebugControlledActionText()
            );
        }

#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD

    private string GetDebugControlledActionText()
    {
        /*
         * These names match the current PoseableLimbBinding setup:
         * Player1 -> LeftArm
         * Player2 -> RightArm
         * Player3 -> LeftLeg
         * Player4 -> RightLeg
         */
        return Slot switch
        {
            PlayerSlot.Player1 =>
                "Currently testing: Left Arm",

            PlayerSlot.Player2 =>
                "Currently testing: Right Arm",

            PlayerSlot.Player3 =>
                "Currently testing: Left Leg",

            PlayerSlot.Player4 =>
                "Currently testing: Right Leg",

            _ =>
                "Currently testing: No limb"
        };
    }

#endif
}