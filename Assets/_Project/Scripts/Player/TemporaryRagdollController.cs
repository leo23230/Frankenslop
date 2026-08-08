using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class TemporaryRagdollController : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private RigBuilder rigBuilder;

    [Header("Ragdoll Root")]
    [Tooltip("Assign the new pelvis bone, or another transform containing only the ragdoll Rigidbody/joint hierarchy. Do not assign the gameplay/network root.")]
    [SerializeField] private Transform ragdollRoot;

    [Tooltip("Usually the new pelvis Rigidbody.")]
    [SerializeField] private Rigidbody pelvisRigidbody;

    [SerializeField] private Rigidbody[] ragdollBodies;
    [SerializeField] private Collider[] ragdollColliders;

    [Header("Fall Reaction")]
    [Tooltip("Leave disabled until the ragdoll is stable under gravity alone.")]
    [SerializeField] private bool applyFallImpulse;
    [SerializeField] private Vector3 fallImpulse = Vector3.zero;
    [SerializeField] private ForceMode fallForceMode = ForceMode.Impulse;
    [SerializeField, Min(1f)] private float ragdollGravityMultiplier = 1.5f;
    [Header("Diagnostics")]
    [SerializeField, Min(0f)] private float anchorWarningDistance = 0.05f;
    [SerializeField] private bool logActivation;

    private Transform[] _bones;
    private Vector3[] _startingLocalPositions;
    private Quaternion[] _startingLocalRotations;
    private bool _isRagdoll;

    public bool IsRagdoll => _isRagdoll;

    private void Awake()
    {
        CollectRagdollPartsIfNeeded();
        ResolvePelvisIfNeeded();
        StoreStartingPose();
        SetRagdollBodiesDynamic(false);
        SetRagdollCollidersEnabled(false);
    }
    private void FixedUpdate()
    {
        if (!_isRagdoll)
            return;

        Vector3 extraGravity =
            Physics.gravity *
            (ragdollGravityMultiplier - 1f);

        foreach (Rigidbody body in ragdollBodies)
        {
            if (body == null ||
                body.isKinematic)
            {
                continue;
            }

            body.AddForce(
                extraGravity,
                ForceMode.Acceleration
            );
        }
    }
    private void CollectRagdollPartsIfNeeded()
    {
        if (ragdollRoot == null)
        {
            Debug.LogError("TemporaryRagdollController requires a dedicated Ragdoll Root.", this);
            return;
        }

        if (ragdollBodies == null || ragdollBodies.Length == 0)
            ragdollBodies = ragdollRoot.GetComponentsInChildren<Rigidbody>(true);

        if (ragdollColliders == null || ragdollColliders.Length == 0)
            ragdollColliders = ragdollRoot.GetComponentsInChildren<Collider>(true);

        ragdollBodies = FilterBodiesUnderRoot(ragdollBodies, ragdollRoot);
        ragdollColliders = FilterCollidersUnderRoot(ragdollColliders, ragdollRoot);
    }

    private void ResolvePelvisIfNeeded()
    {
        if (pelvisRigidbody != null || animator == null || !animator.isHuman)
            return;

        Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        if (hips != null)
            pelvisRigidbody = hips.GetComponent<Rigidbody>();
    }

    private static Rigidbody[] FilterBodiesUnderRoot(Rigidbody[] source, Transform root)
    {
        if (source == null || root == null)
            return System.Array.Empty<Rigidbody>();

        List<Rigidbody> filtered = new();
        foreach (Rigidbody body in source)
        {
            if (body != null && (body.transform == root || body.transform.IsChildOf(root)))
                filtered.Add(body);
        }
        return filtered.ToArray();
    }

    private static Collider[] FilterCollidersUnderRoot(Collider[] source, Transform root)
    {
        if (source == null || root == null)
            return System.Array.Empty<Collider>();

        List<Collider> filtered = new();
        foreach (Collider bodyCollider in source)
        {
            if (bodyCollider != null && (bodyCollider.transform == root || bodyCollider.transform.IsChildOf(root)))
                filtered.Add(bodyCollider);
        }
        return filtered.ToArray();
    }

    private void StoreStartingPose()
    {
        if (animator == null)
            return;

        _bones = animator.GetComponentsInChildren<Transform>(true);
        _startingLocalPositions = new Vector3[_bones.Length];
        _startingLocalRotations = new Quaternion[_bones.Length];

        for (int i = 0; i < _bones.Length; i++)
        {
            _startingLocalPositions[i] = _bones[i].localPosition;
            _startingLocalRotations[i] = _bones[i].localRotation;
        }
    }

    public void BeginRagdoll()
    {
        if (_isRagdoll)
            return;

        _isRagdoll = true;

        if (logActivation)
        {
            Debug.Log(
                "Beginning immediate ragdoll activation.",
                this
            );
        }

        if (rigBuilder != null)
            rigBuilder.enabled = false;

        if (animator != null)
            animator.enabled = false;

        Physics.SyncTransforms();

        SetRagdollCollidersEnabled(true);
        SetRagdollBodiesDynamic(true);

        Physics.SyncTransforms();

        if (applyFallImpulse &&
            pelvisRigidbody != null &&
            fallImpulse.sqrMagnitude > 0f)
        {
            pelvisRigidbody.AddForce(
                transform.TransformDirection(
                    fallImpulse
                ),
                fallForceMode
            );
        }
    }


    public void ResetRagdoll()
    {
        SetRagdollBodiesDynamic(false);
        SetRagdollCollidersEnabled(false);
        RestoreStartingPose();
        Physics.SyncTransforms();

        if (animator != null)
        {
            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f);
        }

        if (rigBuilder != null)
        {
            rigBuilder.enabled = true;
            rigBuilder.Build();
        }

        _isRagdoll = false;
    }

    private void SetRagdollBodiesDynamic(bool dynamic)
    {
        if (ragdollBodies == null)
            return;

        foreach (Rigidbody body in ragdollBodies)
        {
            if (body == null)
                continue;

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = !dynamic;
            body.useGravity = dynamic;

            if (dynamic)
                body.WakeUp();
            else
                body.Sleep();
        }
    }

    private void SetRagdollCollidersEnabled(bool enabled)
    {
        if (ragdollColliders == null)
            return;

        foreach (Collider bodyCollider in ragdollColliders)
        {
            if (bodyCollider != null)
                bodyCollider.enabled = enabled;
        }
    }

    private void RestoreStartingPose()
    {
        if (_bones == null || _startingLocalPositions == null || _startingLocalRotations == null)
            return;

        int count = Mathf.Min(_bones.Length, _startingLocalPositions.Length, _startingLocalRotations.Length);
        for (int i = 0; i < count; i++)
        {
            if (_bones[i] == null)
                continue;

            _bones[i].localPosition = _startingLocalPositions[i];
            _bones[i].localRotation = _startingLocalRotations[i];
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Collect Ragdoll Parts")]
    private void CollectRagdollPartsEditor()
    {
        if (ragdollRoot == null)
        {
            Debug.LogError("Assign Ragdoll Root before collecting parts.", this);
            return;
        }

        ragdollBodies = ragdollRoot.GetComponentsInChildren<Rigidbody>(true);
        ragdollColliders = ragdollRoot.GetComponentsInChildren<Collider>(true);
        ResolvePelvisIfNeeded();
        UnityEditor.EditorUtility.SetDirty(this);

        Debug.Log($"Collected {ragdollBodies.Length} rigidbodies and {ragdollColliders.Length} colliders beneath {ragdollRoot.name}.", this);
    }

    [ContextMenu("Log Ragdoll Joint Connections")]
    private void LogRagdollJointConnections()
    {
        if (ragdollRoot == null)
        {
            Debug.LogError("Assign Ragdoll Root first.", this);
            return;
        }

        Joint[] joints = ragdollRoot.GetComponentsInChildren<Joint>(true);
        foreach (Joint joint in joints)
        {
            Rigidbody ownBody = joint.GetComponent<Rigidbody>();
            string ownBodyName = ownBody != null ? ownBody.name : "MISSING";
            string connectedName = joint.connectedBody != null ? joint.connectedBody.name : "NULL";

            Debug.Log($"Joint: {joint.name} | Own body: {ownBodyName} | Connected body: {connectedName} | Anchor: {joint.anchor} | Connected anchor: {joint.connectedAnchor}", joint);

            if (ownBody == null)
                Debug.LogError($"{joint.name} has a Joint but no Rigidbody.", joint);

            if (ownBody != null && joint.connectedBody == ownBody)
                Debug.LogError($"{joint.name} is connected to its own Rigidbody.", joint);
        }
    }

    [ContextMenu("Check Ragdoll Joint Anchors")]
    private void CheckRagdollJointAnchors()
    {
        if (ragdollRoot == null)
        {
            Debug.LogError("Assign Ragdoll Root first.", this);
            return;
        }

        Joint[] joints = ragdollRoot.GetComponentsInChildren<Joint>(true);
        foreach (Joint joint in joints)
        {
            if (joint.connectedBody == null)
            {
                Debug.LogWarning($"{joint.name} has no Connected Body.", joint);
                continue;
            }

            Vector3 worldAnchor = joint.transform.TransformPoint(joint.anchor);
            Vector3 connectedWorldAnchor = joint.connectedBody.transform.TransformPoint(joint.connectedAnchor);
            float distance = Vector3.Distance(worldAnchor, connectedWorldAnchor);

            Debug.Log($"{joint.name} anchor separation: {distance:0.0000}", joint);

            if (distance > anchorWarningDistance)
                Debug.LogWarning($"{joint.name} begins with separated anchors ({distance:0.0000}). This can launch the ragdoll.", joint);
        }
    }

    [ContextMenu("Validate Ragdoll Root")]
    private void ValidateRagdollRoot()
    {
        if (ragdollRoot == null)
        {
            Debug.LogError("Ragdoll Root is not assigned.", this);
            return;
        }

        Rigidbody controllerBody = GetComponent<Rigidbody>();
        Rigidbody[] foundBodies = ragdollRoot.GetComponentsInChildren<Rigidbody>(true);

        foreach (Rigidbody body in foundBodies)
        {
            if (body == null)
                continue;

            if (controllerBody != null && body == controllerBody)
                Debug.LogError("The gameplay/root Rigidbody is inside the ragdoll collection. Assign a narrower Ragdoll Root.", body);

            Vector3 scale = body.transform.lossyScale;
            bool uniformScale = Mathf.Approximately(scale.x, scale.y) && Mathf.Approximately(scale.y, scale.z);
            if (!uniformScale)
                Debug.LogWarning($"{body.name} has non-uniform world scale {scale}. This can destabilize ragdoll joints.", body);
        }

        Debug.Log("Ragdoll root validation complete.", this);
    }
#endif
}