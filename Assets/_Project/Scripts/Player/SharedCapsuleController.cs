using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SharedCapsuleController : NetworkBehaviour
{
    [Header("Locomotion")]
    [SerializeField, Min(0f)]
    private float moveSpeed = 4f;

    [SerializeField, Min(0f)]
    private float turnSpeed = 120f;

    [Header("Gravity")]
    [SerializeField, Min(0f)]
    private float gravity = 20f;

    [SerializeField, Min(0f)]
    private float groundedForce = 2f;

    [Header("Balance Prototype")]
    [SerializeField]
    private float balanceInput;

    [SerializeField]
    private float balanceAmount;

    [SerializeField, Min(0f)]
    private float balanceResponseSpeed = 5f;

    [Header("Current Network Inputs")]
    [SerializeField]
    private float forwardInput;

    [SerializeField]
    private float turningInput;

    [SerializeField]
    private bool actionHeld;

    private CharacterController _characterController;
    private float _verticalVelocity;

    private void Awake()
    {
        _characterController =
            GetComponent<CharacterController>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        TimeManager.OnTick += HandleServerTick;
    }

    public override void OnStopServer()
    {
        TimeManager.OnTick -= HandleServerTick;

        base.OnStopServer();
    }

    private void HandleServerTick()
    {
        ReadRoleInputs();

        float deltaTime = (float)TimeManager.TickDelta;

        ApplyTurning(deltaTime);
        UpdateBalance(deltaTime);
        UpdateGravity(deltaTime);
        ApplyMovement(deltaTime);
        ProcessActions();
        ClearOneTickInputFlags();
    }

    private void ReadRoleInputs()
    {
        forwardInput = 0f;
        turningInput = 0f;
        balanceInput = 0f;
        actionHeld = false;

        foreach (PlayerControlChannel channel
                 in PlayerControlChannel.ServerChannels)
        {
            if (channel == null)
                continue;

            switch (channel.Role)
            {
                case PlayerControlRole.ForwardBackward:
                    forwardInput =
                        channel.ServerAxisInput.y;
                    break;

                case PlayerControlRole.Turning:
                    turningInput =
                        channel.ServerAxisInput.x;
                    break;

                case PlayerControlRole.LeftArm:
                    balanceInput =
                        channel.ServerAxisInput.x;
                    break;

                case PlayerControlRole.RightArm:
                    actionHeld =
                        channel.ServerActionHeld;
                    break;
            }
        }
    }

    private void ApplyTurning(float deltaTime)
    {
        float turnAmount =
            turningInput * turnSpeed * deltaTime;

        transform.Rotate(
            0f,
            turnAmount,
            0f,
            Space.World
        );
    }

    private void ApplyMovement(float deltaTime)
    {
        Vector3 horizontalVelocity =
            transform.forward *
            forwardInput *
            moveSpeed;

        Vector3 velocity = horizontalVelocity;
        velocity.y = _verticalVelocity;

        _characterController.Move(
            velocity * deltaTime
        );
    }

    private void UpdateGravity(float deltaTime)
    {
        if (_characterController.isGrounded &&
            _verticalVelocity < 0f)
        {
            _verticalVelocity = -groundedForce;
        }
        else
        {
            _verticalVelocity -= gravity * deltaTime;
        }
    }

    private void UpdateBalance(float deltaTime)
    {
        // For now this is only a smoothed server-side value.
        // Later it will feed the humanoid's pelvis lean,
        // center-of-mass target, and balance controller.

        balanceAmount = Mathf.MoveTowards(
            balanceAmount,
            balanceInput,
            balanceResponseSpeed * deltaTime
        );
    }

    private void ProcessActions()
    {
        foreach (PlayerControlChannel channel
                 in PlayerControlChannel.ServerChannels)
        {
            if (channel == null)
                continue;

            if (channel.Role != PlayerControlRole.RightArm)
                continue;

            if (channel.ServerActionPressedThisTick)
            {
                Debug.Log(
                    "Shared character action pressed.",
                    this
                );

                OnSharedActionPressed();
            }
        }
    }

    private void OnSharedActionPressed()
    {
        // Temporary prototype behavior.
        // Replace this later with jump, grab, brace,
        // interact, or another shared-body action.

        Debug.Log(
            "Action executed on the server.",
            this
        );
    }

    private void ClearOneTickInputFlags()
    {
        foreach (PlayerControlChannel channel
                 in PlayerControlChannel.ServerChannels)
        {
            if (channel != null)
                channel.ClearTickFlags();
        }
    }
}