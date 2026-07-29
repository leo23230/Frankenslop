using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class TemporaryRagdollController : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private RigBuilder rigBuilder;

    [Header("Ragdoll")]
    [Tooltip("Usually the hips or pelvis Rigidbody.")]
    [SerializeField]
    private Rigidbody pelvisRigidbody;

    [SerializeField]
    private Rigidbody[] ragdollBodies;

    [SerializeField]
    private Collider[] ragdollColliders;

    [Header("Fall Reaction")]
    [SerializeField]
    private Vector3 fallImpulse = new(1.5f, 0.5f, 0f);

    [SerializeField]
    private ForceMode fallForceMode = ForceMode.Impulse;

    [Header("Reset")]
    [SerializeField, Min(0f)]
    private float ragdollDuration = 3f;

    private Transform[] _bones;
    private Vector3[] _startingLocalPositions;
    private Quaternion[] _startingLocalRotations;

    private bool _isRagdoll;

    public bool IsRagdoll => _isRagdoll;

    private void Awake()
    {
        CollectRagdollPartsIfNeeded();
        StoreStartingPose();
        SetRagdollEnabled(false);
    }

    private void CollectRagdollPartsIfNeeded()
    {
        if (ragdollBodies == null ||
            ragdollBodies.Length == 0)
        {
            ragdollBodies =
                GetComponentsInChildren<Rigidbody>(true);
        }

        if (ragdollColliders == null ||
            ragdollColliders.Length == 0)
        {
            ragdollColliders =
                GetComponentsInChildren<Collider>(true);
        }

        if (pelvisRigidbody == null &&
            animator != null &&
            animator.isHuman)
        {
            Transform hips =
                animator.GetBoneTransform(
                    HumanBodyBones.Hips
                );

            if (hips != null)
            {
                pelvisRigidbody =
                    hips.GetComponent<Rigidbody>();
            }
        }
    }

    private void StoreStartingPose()
    {
        if (animator == null)
            return;

        _bones =
            animator.GetComponentsInChildren<Transform>(true);

        _startingLocalPositions =
            new Vector3[_bones.Length];

        _startingLocalRotations =
            new Quaternion[_bones.Length];

        for (int i = 0; i < _bones.Length; i++)
        {
            _startingLocalPositions[i] =
                _bones[i].localPosition;

            _startingLocalRotations[i] =
                _bones[i].localRotation;
        }
    }

    public void BeginRagdoll()
    {
        if (_isRagdoll)
            return;

        _isRagdoll = true;

        SetRagdollEnabled(true);

        if (pelvisRigidbody != null)
        {
            Vector3 worldImpulse =
                transform.TransformDirection(fallImpulse);

            pelvisRigidbody.AddForce(
                worldImpulse,
                fallForceMode
            );
        }
    }

    public IEnumerator BeginRagdollAndWait()
    {
        BeginRagdoll();

        yield return new WaitForSeconds(
            ragdollDuration
        );
    }

    public void ResetRagdoll()
    {
        SetRagdollEnabled(false);
        RestoreStartingPose();

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        if (rigBuilder != null)
        {
            rigBuilder.Build();
        }

        _isRagdoll = false;
    }

    private void SetRagdollEnabled(bool enabled)
    {
        if (animator != null)
            animator.enabled = !enabled;

        if (rigBuilder != null)
            rigBuilder.enabled = !enabled;

        if (ragdollBodies != null)
        {
            foreach (Rigidbody body in ragdollBodies)
            {
                if (body == null)
                    continue;

                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = !enabled;
                body.useGravity = enabled;
            }
        }

        if (ragdollColliders != null)
        {
            foreach (Collider bodyCollider
                     in ragdollColliders)
            {
                if (bodyCollider == null)
                    continue;

                bodyCollider.enabled = enabled;
            }
        }
    }

    private void RestoreStartingPose()
    {
        if (_bones == null)
            return;

        for (int i = 0; i < _bones.Length; i++)
        {
            if (_bones[i] == null)
                continue;

            _bones[i].localPosition =
                _startingLocalPositions[i];

            _bones[i].localRotation =
                _startingLocalRotations[i];
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Collect Ragdoll Parts")]
    private void CollectRagdollPartsEditor()
    {
        ragdollBodies =
            GetComponentsInChildren<Rigidbody>(true);

        ragdollColliders =
            GetComponentsInChildren<Collider>(true);

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
