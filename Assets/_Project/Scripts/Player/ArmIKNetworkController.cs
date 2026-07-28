using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class ArmIKNetworkController : NetworkBehaviour
{
    [Header("Target Space")]
    [SerializeField]
    private Transform targetSpace;

    [Header("IK Targets")]
    [SerializeField]
    private Transform leftHandTarget;

    [SerializeField]
    private Transform rightHandTarget;

    [Header("Target Movement")]
    [SerializeField, Min(0f)]
    private float targetMoveSpeed = 1.5f;

    [SerializeField, Min(0f)]
    private float visualSmoothing = 15f;

    [Header("Left Hand Bounds")]
    [SerializeField]
    private Vector3 leftMinimum = new(-1.1f, 0.7f, 0.4f);

    [SerializeField]
    private Vector3 leftMaximum = new(-0.1f, 2f, 0.4f);

    [Header("Right Hand Bounds")]
    [SerializeField]
    private Vector3 rightMinimum = new(0.1f, 0.7f, 0.4f);

    [SerializeField]
    private Vector3 rightMaximum = new(1.1f, 2f, 0.4f);

    private readonly SyncVar<Vector3> _leftTargetPosition = new();
    private readonly SyncVar<Vector3> _rightTargetPosition = new();

    private float _leftFixedDepth;
    private float _rightFixedDepth;

    public override void OnStartServer()
    {
        base.OnStartServer();

        Vector3 leftStartingPosition =
            targetSpace.InverseTransformPoint(leftHandTarget.position);

        Vector3 rightStartingPosition =
            targetSpace.InverseTransformPoint(rightHandTarget.position);

        _leftFixedDepth = leftStartingPosition.z;
        _rightFixedDepth = rightStartingPosition.z;

        leftStartingPosition.z = _leftFixedDepth;
        rightStartingPosition.z = _rightFixedDepth;

        _leftTargetPosition.Value = leftStartingPosition;
        _rightTargetPosition.Value = rightStartingPosition;

        TimeManager.OnTick += HandleServerTick;
    }

    public override void OnStopServer()
    {
        TimeManager.OnTick -= HandleServerTick;

        base.OnStopServer();
    }

    private void Update()
    {
        ApplyTargetPositions();
    }

    private void HandleServerTick()
    {
        Vector2 leftArmInput = Vector2.zero;
        Vector2 rightArmInput = Vector2.zero;

        foreach (PlayerControlChannel channel
                 in PlayerControlChannel.ServerChannels)
        {
            if (channel == null)
                continue;

            switch (channel.Role)
            {
                case PlayerControlRole.LeftArm:
                    leftArmInput = channel.ServerAxisInput;
                    break;

                case PlayerControlRole.RightArm:
                    rightArmInput = channel.ServerAxisInput;
                    break;
            }
        }

        float deltaTime = (float)TimeManager.TickDelta;

        _leftTargetPosition.Value = MoveTargetInPlane(
            _leftTargetPosition.Value,
            leftArmInput,
            _leftFixedDepth,
            leftMinimum,
            leftMaximum,
            deltaTime
        );

        _rightTargetPosition.Value = MoveTargetInPlane(
            _rightTargetPosition.Value,
            rightArmInput,
            _rightFixedDepth,
            rightMinimum,
            rightMaximum,
            deltaTime
        );
    }

    private Vector3 MoveTargetInPlane(
        Vector3 currentPosition,
        Vector2 input,
        float fixedDepth,
        Vector3 minimum,
        Vector3 maximum,
        float deltaTime)
    {
        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 movement = new Vector3(
            input.x,
            input.y,
            0f
        );

        currentPosition +=
            movement * targetMoveSpeed * deltaTime;

        currentPosition.x = Mathf.Clamp(
            currentPosition.x,
            minimum.x,
            maximum.x
        );

        currentPosition.y = Mathf.Clamp(
            currentPosition.y,
            minimum.y,
            maximum.y
        );

        // Keep the hand on a fixed front/back plane.
        currentPosition.z = fixedDepth;

        return currentPosition;
    }

    private void ApplyTargetPositions()
    {
        if (targetSpace == null ||
            leftHandTarget == null ||
            rightHandTarget == null)
        {
            return;
        }

        float smoothingAmount =
            1f - Mathf.Exp(
                -visualSmoothing * Time.deltaTime
            );

        leftHandTarget.localPosition = Vector3.Lerp(
            leftHandTarget.localPosition,
            _leftTargetPosition.Value,
            smoothingAmount
        );

        rightHandTarget.localPosition = Vector3.Lerp(
            rightHandTarget.localPosition,
            _rightTargetPosition.Value,
            smoothingAmount
        );
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (targetSpace == null)
            return;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = targetSpace.localToWorldMatrix;

        DrawMovementArea(leftMinimum, leftMaximum);
        DrawMovementArea(rightMinimum, rightMaximum);

        Gizmos.matrix = previousMatrix;
    }

    private static void DrawMovementArea(
        Vector3 minimum,
        Vector3 maximum)
    {
        Vector3 center = new Vector3(
            (minimum.x + maximum.x) * 0.5f,
            (minimum.y + maximum.y) * 0.5f,
            minimum.z
        );

        Vector3 size = new Vector3(
            maximum.x - minimum.x,
            maximum.y - minimum.y,
            0.01f
        );

        Gizmos.DrawWireCube(center, size);
    }
#endif
}