using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PoseEvaluationTrigger : NetworkBehaviour
{
    [Header("Debug")]
    [SerializeField]
    private bool logEvaluations = true;

    private Collider _triggerCollider;

    private void Awake()
    {
        _triggerCollider =
            GetComponent<Collider>();

        if (!_triggerCollider.isTrigger)
        {
            Debug.LogWarning(
                $"{name}'s collider was not marked as a trigger. " +
                "Enabling Is Trigger automatically.",
                this
            );

            _triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(
        Collider other)
    {
        /*
         * The trigger may also exist on clients, but scoring must
         * happen only on the authoritative server.
         */
        if (!IsServerInitialized)
            return;

        ServerPoseWall wall =
            other.GetComponentInParent<
                ServerPoseWall
            >();

        if (wall == null)
            return;

        if (wall.HasBeenEvaluated)
            return;

        if (logEvaluations)
        {
            Debug.Log(
                $"Server evaluation trigger reached by {wall.name}.",
                wall
            );
        }

        wall.ServerEvaluateAtTrigger();
    }
}