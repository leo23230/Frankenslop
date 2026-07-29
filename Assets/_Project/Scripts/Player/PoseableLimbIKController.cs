using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class PoseableLimbIKController : NetworkBehaviour
{
    [Header("Player Bindings")]
    [SerializeField]
    private PoseableLimbBinding[] bindings =
        new PoseableLimbBinding[4];

    [Header("Target Space")]
    [Tooltip(
        "All four IK targets should be direct children of this transform."
    )]

    [Header("Ragdoll Reaction")]
    [SerializeField]
    private TemporaryRagdollController ragdollController;

    [SerializeField, Min(0f)]
    private float resetDelay = 3f;

    private Coroutine _fallRoutine;

    [SerializeField]
    private Transform targetSpace;

    [Header("Hand Targets")]
    [SerializeField]
    private Transform leftHandTarget;

    [SerializeField]
    private Transform rightHandTarget;

    [Header("Foot Targets")]
    [SerializeField]
    private Transform leftFootTarget;

    [SerializeField]
    private Transform rightFootTarget;

    [Header("Target Movement")]
    [SerializeField, Min(0f)]
    private float handMoveSpeed = 1.5f;

    [SerializeField, Min(0f)]
    private float footMoveSpeed = 1f;

    [SerializeField, Min(0f)]
    private float visualSmoothing = 15f;

    [Header("Left Hand Bounds")]
    [SerializeField]
    private Vector2 leftHandMinimum =
        new(-1.2f, 0.7f);

    [SerializeField]
    private Vector2 leftHandMaximum =
        new(-0.05f, 2.1f);

    [Header("Right Hand Bounds")]
    [SerializeField]
    private Vector2 rightHandMinimum =
        new(0.05f, 0.7f);

    [SerializeField]
    private Vector2 rightHandMaximum =
        new(1.2f, 2.1f);

    [Header("Left Foot Bounds")]
    [SerializeField]
    private Vector2 leftFootMinimum =
        new(-0.8f, 0f);

    [SerializeField]
    private Vector2 leftFootMaximum =
        new(-0.05f, 1f);

    [Header("Right Foot Bounds")]
    [SerializeField]
    private Vector2 rightFootMinimum =
        new(0.05f, 0f);

    [SerializeField]
    private Vector2 rightFootMaximum =
        new(0.8f, 1f);

    [Header("Foot Support")]
    [Tooltip(
        "How far above its starting floor height a foot may be " +
        "while still counting as planted."
    )]
    [SerializeField, Min(0f)]
    private float plantedHeightTolerance = 0.08f;

    [Tooltip(
        "Both feet must be unsupported for this long before falling."
    )]
    [SerializeField, Min(0f)]
    private float unsupportedGraceTime = 0.25f;

    [Header("Simple Fall Visual")]
    [Tooltip(
        "Rotate only the model/rig visual hierarchy, not the NetworkObject root."
    )]
    [SerializeField]
    private Transform visualRoot;

    [SerializeField]
    private Vector3 fallenLocalEulerAngles =
        new(0f, 0f, 75f);

    [SerializeField, Min(0f)]
    private float fallRotationSpeed = 180f;

    /*
     * Positions are stored in targetSpace-local coordinates.
     */
    private readonly SyncVar<Vector3> _leftHandPosition =
        new();

    private readonly SyncVar<Vector3> _rightHandPosition =
        new();

    private readonly SyncVar<Vector3> _leftFootPosition =
        new();

    private readonly SyncVar<Vector3> _rightFootPosition =
        new();

    private readonly SyncVar<bool> _isFallen =
        new(false);

    private float _leftHandDepth;
    private float _rightHandDepth;
    private float _leftFootDepth;
    private float _rightFootDepth;

    private float _leftFootFloorHeight;
    private float _rightFootFloorHeight;

    private float _unsupportedTimer;

    private Quaternion _standingVisualRotation;

    public bool IsFallen => _isFallen.Value;

    private Vector3 _startingLeftHandPosition;
    private Vector3 _startingRightHandPosition;
    private Vector3 _startingLeftFootPosition;
    private Vector3 _startingRightFootPosition;

    private bool _startingPoseStored;

    public override void OnStartServer()
    {
        base.OnStartServer();

        InitializeAuthoritativeState();

        TimeManager.OnTick += HandleServerTick;
    }

    public override void OnStopServer()
    {
        TimeManager.OnTick -= HandleServerTick;

        base.OnStopServer();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (visualRoot != null)
        {
            _standingVisualRotation =
                visualRoot.localRotation;
        }
    }

    private void Awake()
    {
        if (visualRoot != null)
        {
            _standingVisualRotation =
                visualRoot.localRotation;
        }
    }

    private void Update()
    {
        ApplyReplicatedTargetPositions();
        ApplyFallVisual();
    }

    private void InitializeAuthoritativeState()
    {
        if (!ValidateRequiredReferences())
            return;

        if (!_startingPoseStored)
        {
            _startingLeftHandPosition =
                GetTargetSpacePosition(leftHandTarget);

            _startingRightHandPosition =
                GetTargetSpacePosition(rightHandTarget);

            _startingLeftFootPosition =
                GetTargetSpacePosition(leftFootTarget);

            _startingRightFootPosition =
                GetTargetSpacePosition(rightFootTarget);

            _startingPoseStored = true;
        }

        _leftHandPosition.Value =
            _startingLeftHandPosition;

        _rightHandPosition.Value =
            _startingRightHandPosition;

        _leftFootPosition.Value =
            _startingLeftFootPosition;

        _rightFootPosition.Value =
            _startingRightFootPosition;

        _leftHandDepth =
            _startingLeftHandPosition.z;

        _rightHandDepth =
            _startingRightHandPosition.z;

        _leftFootDepth =
            _startingLeftFootPosition.z;

        _rightFootDepth =
            _startingRightFootPosition.z;

        _leftFootFloorHeight =
            _startingLeftFootPosition.y;

        _rightFootFloorHeight =
            _startingRightFootPosition.y;

        _unsupportedTimer = 0f;
        _isFallen.Value = false;
    }

    private void HandleServerTick()
    {
        if (_isFallen.Value)
            return;

        if (!ValidateRequiredReferences())
            return;

        ReadPlayerInputs(
            out Vector2 leftArmInput,
            out Vector2 rightArmInput,
            out Vector2 leftLegInput,
            out Vector2 rightLegInput
        );

        float deltaTime =
            (float)TimeManager.TickDelta;

        _leftHandPosition.Value = MoveTargetInPlane(
            _leftHandPosition.Value,
            leftArmInput,
            leftHandMinimum,
            leftHandMaximum,
            _leftHandDepth,
            handMoveSpeed,
            deltaTime
        );

        _rightHandPosition.Value = MoveTargetInPlane(
            _rightHandPosition.Value,
            rightArmInput,
            rightHandMinimum,
            rightHandMaximum,
            _rightHandDepth,
            handMoveSpeed,
            deltaTime
        );

        _leftFootPosition.Value = MoveTargetInPlane(
            _leftFootPosition.Value,
            leftLegInput,
            leftFootMinimum,
            leftFootMaximum,
            _leftFootDepth,
            footMoveSpeed,
            deltaTime
        );

        _rightFootPosition.Value = MoveTargetInPlane(
            _rightFootPosition.Value,
            rightLegInput,
            rightFootMinimum,
            rightFootMaximum,
            _rightFootDepth,
            footMoveSpeed,
            deltaTime
        );

        UpdateSupportState(deltaTime);
    }

    private void ReadPlayerInputs(
        out Vector2 leftArmInput,
        out Vector2 rightArmInput,
        out Vector2 leftLegInput,
        out Vector2 rightLegInput)
    {
        leftArmInput = Vector2.zero;
        rightArmInput = Vector2.zero;
        leftLegInput = Vector2.zero;
        rightLegInput = Vector2.zero;

        foreach (PlayerControlChannel channel
                 in PlayerControlChannel.ServerChannels)
        {
            if (channel == null)
                continue;

            PoseableLimbAction assignedAction =
                GetActionForSlot(channel.Slot);

            Vector2 input =
                channel.RawServerAxisInput;

            switch (assignedAction)
            {
                case PoseableLimbAction.LeftArm:
                    leftArmInput = input;
                    break;

                case PoseableLimbAction.RightArm:
                    rightArmInput = input;
                    break;

                case PoseableLimbAction.LeftLeg:
                    leftLegInput = input;
                    break;

                case PoseableLimbAction.RightLeg:
                    rightLegInput = input;
                    break;
            }
        }
    }

    private PoseableLimbAction GetActionForSlot(
        PlayerSlot slot)
    {
        if (bindings == null)
            return PoseableLimbAction.None;

        foreach (PoseableLimbBinding binding in bindings)
        {
            if (binding.PlayerSlot == slot)
                return binding.Action;
        }

        return PoseableLimbAction.None;
    }

    private static Vector3 MoveTargetInPlane(
        Vector3 currentPosition,
        Vector2 input,
        Vector2 minimum,
        Vector2 maximum,
        float fixedDepth,
        float moveSpeed,
        float deltaTime)
    {
        input =
            Vector2.ClampMagnitude(input, 1f);

        currentPosition.x +=
            input.x *
            moveSpeed *
            deltaTime;

        currentPosition.y +=
            input.y *
            moveSpeed *
            deltaTime;

        currentPosition.x =
            Mathf.Clamp(
                currentPosition.x,
                minimum.x,
                maximum.x
            );

        currentPosition.y =
            Mathf.Clamp(
                currentPosition.y,
                minimum.y,
                maximum.y
            );

        currentPosition.z =
            fixedDepth;

        return currentPosition;
    }

    private void UpdateSupportState(float deltaTime)
    {
        bool leftFootSupported =
            IsFootSupported(
                _leftFootPosition.Value,
                _leftFootFloorHeight
            );

        bool rightFootSupported =
            IsFootSupported(
                _rightFootPosition.Value,
                _rightFootFloorHeight
            );

        bool bothFeetUnsupported =
            !leftFootSupported &&
            !rightFootSupported;

        if (!bothFeetUnsupported)
        {
            _unsupportedTimer = 0f;
            return;
        }

        _unsupportedTimer += deltaTime;

        if (_unsupportedTimer >= unsupportedGraceTime)
            TriggerFall();
    }

    private bool IsFootSupported(
        Vector3 footPosition,
        float floorHeight)
    {
        float maximumSupportedHeight =
            floorHeight +
            plantedHeightTolerance;

        return footPosition.y <=
               maximumSupportedHeight;
    }

    [ObserversRpc]
    private void TriggerFallObserversRpc()
    {
        if (ragdollController != null)
            ragdollController.BeginRagdoll();
    }
    [ObserversRpc]
    private void ResetBodyObserversRpc()
    {
        if (ragdollController != null)
            ragdollController.ResetRagdoll();
    }

    [Server]
    private void TriggerFall()
    {
        if (_isFallen.Value)
            return;

        _isFallen.Value = true;
        _unsupportedTimer = 0f;

        TriggerFallObserversRpc();

        if (_fallRoutine != null)
            StopCoroutine(_fallRoutine);

        _fallRoutine =
            StartCoroutine(ServerResetAfterDelay());

        Debug.Log(
            "Both feet lost support. Starting ragdoll.",
            this
        );
    }

    private System.Collections.IEnumerator
    ServerResetAfterDelay()
    {
        yield return new WaitForSeconds(resetDelay);

        ResetBody();

        ResetBodyObserversRpc();

        _fallRoutine = null;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestResetServerRpc()
    {
        ResetBody();
    }

    [Server]
    public void ResetBody()
    {
        if (!ValidateRequiredReferences())
            return;

        InitializeAuthoritativeState();
    }

    private Vector3 GetTargetSpacePosition(
        Transform target)
    {
        return targetSpace.InverseTransformPoint(
            target.position
        );
    }

    private void ApplyReplicatedTargetPositions()
    {
        if (targetSpace == null)
            return;

        float interpolationAmount =
            1f -
            Mathf.Exp(
                -visualSmoothing *
                Time.deltaTime
            );

        ApplyTargetPosition(
            leftHandTarget,
            _leftHandPosition.Value,
            interpolationAmount
        );

        ApplyTargetPosition(
            rightHandTarget,
            _rightHandPosition.Value,
            interpolationAmount
        );

        ApplyTargetPosition(
            leftFootTarget,
            _leftFootPosition.Value,
            interpolationAmount
        );

        ApplyTargetPosition(
            rightFootTarget,
            _rightFootPosition.Value,
            interpolationAmount
        );
    }

    private static void ApplyTargetPosition(
        Transform target,
        Vector3 targetLocalPosition,
        float interpolationAmount)
    {
        if (target == null)
            return;

        target.localPosition =
            Vector3.Lerp(
                target.localPosition,
                targetLocalPosition,
                interpolationAmount
            );
    }

    private void ApplyFallVisual()
    {
        if (visualRoot == null)
            return;

        Quaternion targetRotation =
            _isFallen.Value
                ? _standingVisualRotation *
                  Quaternion.Euler(
                      fallenLocalEulerAngles
                  )
                : _standingVisualRotation;

        visualRoot.localRotation =
            Quaternion.RotateTowards(
                visualRoot.localRotation,
                targetRotation,
                fallRotationSpeed *
                Time.deltaTime
            );
    }

    private bool ValidateRequiredReferences()
    {
        if (targetSpace == null)
        {
            Debug.LogError(
                "Poseable body has no Target Space assigned.",
                this
            );

            return false;
        }

        if (leftHandTarget == null ||
            rightHandTarget == null ||
            leftFootTarget == null ||
            rightFootTarget == null)
        {
            Debug.LogError(
                "One or more stationary-body IK targets are missing.",
                this
            );

            return false;
        }

        return true;
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        ValidateBindings();
    }

    private void ValidateBindings()
    {
        if (bindings == null)
            return;

        for (int i = 0; i < bindings.Length; i++)
        {
            PoseableLimbBinding current =
                bindings[i];

            if (current.PlayerSlot == PlayerSlot.None)
                continue;

            for (int j = i + 1; j < bindings.Length; j++)
            {
                PoseableLimbBinding other =
                    bindings[j];

                if (current.PlayerSlot ==
                    other.PlayerSlot)
                {
                    Debug.LogWarning(
                        $"{current.PlayerSlot} is assigned " +
                        $"more than once.",
                        this
                    );
                }

                if (current.Action !=
                        PoseableLimbAction.None &&
                    current.Action ==
                        other.Action)
                {
                    Debug.LogWarning(
                        $"{current.Action} is assigned " +
                        $"to more than one player.",
                        this
                    );
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (targetSpace == null)
            return;

        Matrix4x4 previousMatrix =
            Gizmos.matrix;

        Gizmos.matrix =
            targetSpace.localToWorldMatrix;

        DrawControlArea(
            leftHandMinimum,
            leftHandMaximum,
            GetEditorDepth(leftHandTarget)
        );

        DrawControlArea(
            rightHandMinimum,
            rightHandMaximum,
            GetEditorDepth(rightHandTarget)
        );

        DrawControlArea(
            leftFootMinimum,
            leftFootMaximum,
            GetEditorDepth(leftFootTarget)
        );

        DrawControlArea(
            rightFootMinimum,
            rightFootMaximum,
            GetEditorDepth(rightFootTarget)
        );

        Gizmos.matrix =
            previousMatrix;
    }

    private float GetEditorDepth(
        Transform target)
    {
        if (target == null ||
            targetSpace == null)
        {
            return 0f;
        }

        return targetSpace
            .InverseTransformPoint(target.position)
            .z;
    }

    private static void DrawControlArea(
        Vector2 minimum,
        Vector2 maximum,
        float depth)
    {
        Vector3 center = new(
            (minimum.x + maximum.x) * 0.5f,
            (minimum.y + maximum.y) * 0.5f,
            depth
        );

        Vector3 size = new(
            maximum.x - minimum.x,
            maximum.y - minimum.y,
            0.01f
        );

        Gizmos.DrawWireCube(
            center,
            size
        );
    }

#endif
}
