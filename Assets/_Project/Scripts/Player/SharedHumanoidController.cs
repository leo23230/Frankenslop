using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SharedHumanoidController : NetworkBehaviour
{
    [Header("Locomotion")]
    [SerializeField, Min(0f)]
    private float moveSpeed = 4f;

    [SerializeField, Min(0f)]
    private float turnSpeed = 120f;

    [Header("Acceleration")]
    [SerializeField, Min(0f)]
    private float acceleration = 12f;

    [SerializeField, Min(0f)]
    private float deceleration = 16f;

    [SerializeField, Min(0f)]
    private float turningAcceleration = 10f;

    [Header("Gravity")]
    [SerializeField, Min(0f)]
    private float gravity = 20f;

    [SerializeField, Min(0f)]
    private float groundedForce = 2f;

    [Header("Current Inputs")]
    [SerializeField, Range(-1f, 1f)]
    private float forwardInput;

    [SerializeField, Range(-1f, 1f)]
    private float turningInput;

    [Header("Runtime Movement")]
    [SerializeField]
    private float currentForwardSpeed;

    [SerializeField]
    private float currentTurnSpeed;

    [SerializeField]
    private float verticalVelocity;

    private CharacterController _characterController;

    public float ForwardInput => forwardInput;
    public float TurningInput => turningInput;
    public float CurrentForwardSpeed => currentForwardSpeed;
    public float CurrentTurnSpeed => currentTurnSpeed;

    public Vector3 HorizontalVelocity
    {
        get
        {
            Vector3 velocity = _characterController != null
                ? _characterController.velocity
                : Vector3.zero;

            velocity.y = 0f;
            return velocity;
        }
    }

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        TimeManager.OnTick += HandleServerTick;
    }

    public override void OnStopServer()
    {
        TimeManager.OnTick -= HandleServerTick;

        forwardInput = 0f;
        turningInput = 0f;
        currentForwardSpeed = 0f;
        currentTurnSpeed = 0f;

        base.OnStopServer();
    }

    private void HandleServerTick()
    {
        float deltaTime = (float)TimeManager.TickDelta;

        ReadRoleInputs();
        UpdateMovementSpeeds(deltaTime);
        ApplyTurning(deltaTime);
        UpdateGravity(deltaTime);
        ApplyMovement(deltaTime);
    }

    private void ReadRoleInputs()
    {
        forwardInput = 0f;
        turningInput = 0f;

        foreach (PlayerControlChannel channel
                 in PlayerControlChannel.ServerChannels)
        {
            if (channel == null)
                continue;

            switch (channel.Role)
            {
                case PlayerControlRole.ForwardBackward:
                    forwardInput = Mathf.Clamp(
                        channel.ServerAxisInput.y,
                        -1f,
                        1f
                    );
                    break;

                case PlayerControlRole.Turning:
                    turningInput = Mathf.Clamp(
                        channel.ServerAxisInput.x,
                        -1f,
                        1f
                    );
                    break;
            }
        }
    }

    private void UpdateMovementSpeeds(float deltaTime)
    {
        float targetForwardSpeed = forwardInput * moveSpeed;
        float targetTurnSpeed = turningInput * turnSpeed;

        float forwardChangeRate =
            Mathf.Abs(targetForwardSpeed) >
            Mathf.Abs(currentForwardSpeed)
                ? acceleration
                : deceleration;

        currentForwardSpeed = Mathf.MoveTowards(
            currentForwardSpeed,
            targetForwardSpeed,
            forwardChangeRate * deltaTime
        );

        currentTurnSpeed = Mathf.MoveTowards(
            currentTurnSpeed,
            targetTurnSpeed,
            turningAcceleration * turnSpeed * deltaTime
        );
    }

    private void ApplyTurning(float deltaTime)
    {
        float rotationAmount =
            currentTurnSpeed * deltaTime;

        transform.Rotate(
            0f,
            rotationAmount,
            0f,
            Space.World
        );
    }

    private void UpdateGravity(float deltaTime)
    {
        if (_characterController.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -groundedForce;
        }
        else
        {
            verticalVelocity -= gravity * deltaTime;
        }
    }

    private void ApplyMovement(float deltaTime)
    {
        Vector3 horizontalVelocity =
            transform.forward * currentForwardSpeed;

        Vector3 finalVelocity = horizontalVelocity;
        finalVelocity.y = verticalVelocity;

        _characterController.Move(
            finalVelocity * deltaTime
        );
    }
}