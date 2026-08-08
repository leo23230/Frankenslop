using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using System;


public class ServerPoseWall : NetworkBehaviour
{
    //events
    public static event Action<ServerPoseWall> ServerWallFailed;

    [Header("Pose Evaluation")]
    [SerializeField]
    private PoseWallJudge poseWallJudge;

    [Header("Wall Sequence")]
    [Tooltip(
        "Enable this only on the first wall in the sequence. Later " +
        "walls remain hidden and stationary until the wall ahead passes."
    )]
    [SerializeField]
    private bool startsRevealed;

    [Tooltip(
        "The wall behind this one. It is revealed when this wall gets " +
        "Pass, Good, or Perfect."
    )]
    [SerializeField]
    private ServerPoseWall nextWall;

    [Header("Wall State")]
    [SerializeField]
    private bool movementEnabled = true;

    private readonly SyncVar<bool> _isRevealed =
        new(false);

    [Header("Fade After Evaluation")]
    [SerializeField]
    private bool fadeAfterEvaluation = true;

    [SerializeField, Min(0f)]
    private float fadeDelay = 0.15f;

    [SerializeField, Min(0.01f)]
    private float fadeDuration = 0.8f;

    [Tooltip(
        "Renderers belonging to this wall. Leave empty to collect " +
        "all renderers in the wall's children."
    )]
    [SerializeField]
    private Renderer[] wallRenderers;

    [SerializeField]
    private bool disableRenderersAfterFade = true;

    [Header("Collision")]
    [Tooltip(
        "Colliders that physically block the player. Leave empty to " +
        "collect all non-trigger colliders in the wall's children."
    )]
    [SerializeField]
    private Collider[] blockingColliders;

    [Tooltip(
        "Disable blocking colliders immediately when the team passes."
    )]
    [SerializeField]
    private bool disableCollidersOnPass = true;

    private readonly List<Material> _fadeMaterials =
        new();

    private Vector3 _startingPosition;
    private Quaternion _startingRotation;

    private bool _hasBeenEvaluated;
    private Coroutine _fadeRoutine;

    public bool MovementEnabled =>
        movementEnabled;

    public bool HasBeenEvaluated =>
        _hasBeenEvaluated;

    public bool IsRevealed =>
        _isRevealed.Value;

    public PoseEvaluationResult LatestResult
    {
        get;
        private set;
    }

    private void Awake()
    {
        _startingPosition =
            transform.position;

        _startingRotation =
            transform.rotation;

        CollectWallRenderers();
        CollectBlockingColliders();
        PrepareFadeMaterials();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        _hasBeenEvaluated = false;
        LatestResult = default;

        _isRevealed.Value =
            startsRevealed;

        movementEnabled =
            startsRevealed;

        SetBlockingCollidersEnabled(
            startsRevealed
        );

        ApplyRevealStateObserversRpc(
            startsRevealed
        );
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        ApplyRevealVisuals(
            _isRevealed.Value
        );
    }

    /// <summary>
    /// Moves this wall toward negative world Z.
    /// Called by ServerWallGroupController on the server.
    /// </summary>
    [Server]
    public void ServerMove(float distance)
    {
        if (!movementEnabled ||
            !_isRevealed.Value)
        {
            return;
        }

        transform.position +=
            Vector3.back *
            distance;
    }

    /// <summary>
    /// Evaluates this wall once when it reaches the evaluation trigger.
    /// </summary>
    [Server]
    public void ServerEvaluateAtTrigger()
    {
        if (_hasBeenEvaluated)
            return;

        _hasBeenEvaluated = true;

        if (poseWallJudge == null)
        {
            Debug.LogError(
                $"{name} has no PoseWallJudge assigned.",
                this
            );

            return;
        }

        LatestResult =
            poseWallJudge.EvaluateNow();

        HandleServerResult(
            LatestResult
        );

        if (fadeAfterEvaluation)
        {
            BeginFadeObserversRpc();
        }
    }

    [Server]
    private void HandleServerResult(PoseEvaluationResult result)
    {
        switch (result.OverallGrade)
        {
            case MinigameScoreGrade.Fail:
                HandleFailure(result);
                break;

            case MinigameScoreGrade.Pass:
                HandlePass(result);
                break;

            case MinigameScoreGrade.Good:
                HandleGood(result);
                break;

            case MinigameScoreGrade.Perfect:
                HandlePerfect(result);
                break;
        }
    }

    [Server]
    private void HandleFailure(PoseEvaluationResult result)
    {
        Debug.Log($"{name}: FAIL — " +$"{result.OverallNormalizedScore:P1}", this);
        ServerWallFailed?.Invoke(this);
    }

    [Server]
    private void HandlePass(PoseEvaluationResult result)
    {
        Debug.Log($"{name}: PASS — " + $"{result.OverallNormalizedScore:P1}",this);

        if (disableCollidersOnPass)
        {
            SetBlockingCollidersEnabled(false);
        }

        //eventually this will cause the player to get nudged closer to the pool of water

        RevealNextWall();
    }

    [Server]
    private void HandleGood(
        PoseEvaluationResult result)
    {
        Debug.Log($"{name}: GOOD — " +$"{result.OverallNormalizedScore:P1}",this);

        if (disableCollidersOnPass)
        {
            SetBlockingCollidersEnabled(false);
        }

        RevealNextWall();
    }

    [Server]
    private void HandlePerfect(PoseEvaluationResult result)
    {
        Debug.Log($"{name}: PERFECT — " +$"{result.OverallNormalizedScore:P1}",this);

        if (disableCollidersOnPass)
        {
            SetBlockingCollidersEnabled(false);
        }

        RevealNextWall();
    }

    [Server]
    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
    }

    [Server]
    public void ResetWall()
    {
        transform.SetPositionAndRotation(
            _startingPosition,
            _startingRotation
        );

        ResetServerState();
    }

    [Server]
    public void ResetWall(
        Vector3 position,
        Quaternion rotation)
    {
        transform.SetPositionAndRotation(
            position,
            rotation
        );

        ResetServerState();
    }

    [Server]
    private void ResetServerState()
    {
        _hasBeenEvaluated = false;
        LatestResult = default;

        _isRevealed.Value =
            startsRevealed;

        movementEnabled =
            startsRevealed;

        SetBlockingCollidersEnabled(
            startsRevealed
        );

        if (poseWallJudge != null)
        {
            poseWallJudge.ResetEvaluation();
        }

        ResetFadeVisualsObserversRpc();

        ApplyRevealStateObserversRpc(
            startsRevealed
        );
    }

    [Server]
    private void RevealNextWall()
    {
        if (nextWall == null)
            return;

        nextWall.ServerReveal();
    }

    [Server]
    public void ServerReveal()
    {
        if (_isRevealed.Value)
            return;

        _isRevealed.Value = true;
        movementEnabled = true;

        SetBlockingCollidersEnabled(true);
        ApplyRevealStateObserversRpc(true);
    }

    [ObserversRpc]
    private void ApplyRevealStateObserversRpc(
        bool revealed)
    {
        ApplyRevealVisuals(revealed);
    }

    private void ApplyRevealVisuals(
        bool revealed)
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        SetRenderersEnabled(revealed);
        SetWallAlpha(revealed ? 1f : 0f);
    }

    [ObserversRpc]
    private void BeginFadeObserversRpc()
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
        }

        _fadeRoutine =
            StartCoroutine(FadeWallRoutine());
    }

    private IEnumerator FadeWallRoutine()
    {
        if (fadeDelay > 0f)
        {
            yield return new WaitForSeconds(
                fadeDelay
            );
        }

        if (_fadeMaterials.Count == 0)
        {
            PrepareFadeMaterials();
        }

        float[] startingAlphas =
            new float[_fadeMaterials.Count];

        for (int i = 0;
             i < _fadeMaterials.Count;
             i++)
        {
            Material material =
                _fadeMaterials[i];

            startingAlphas[i] =
                material != null
                    ? GetMaterialColor(material).a
                    : 0f;
        }

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    fadeDuration
                );

            float smoothedProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            for (int i = 0;
                 i < _fadeMaterials.Count;
                 i++)
            {
                Material material =
                    _fadeMaterials[i];

                if (material == null)
                    continue;

                Color color =
                    GetMaterialColor(material);

                color.a =
                    Mathf.Lerp(
                        startingAlphas[i],
                        0f,
                        smoothedProgress
                    );

                SetMaterialColor(
                    material,
                    color
                );
            }

            yield return null;
        }

        SetWallAlpha(0f);

        if (disableRenderersAfterFade)
        {
            SetRenderersEnabled(false);
        }

        _fadeRoutine = null;
    }

    [ObserversRpc]
    private void ResetFadeVisualsObserversRpc()
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        SetRenderersEnabled(true);
        SetWallAlpha(1f);
    }

    private void CollectWallRenderers()
    {
        if (wallRenderers != null &&
            wallRenderers.Length > 0)
        {
            return;
        }

        wallRenderers =
            GetComponentsInChildren<Renderer>(true);
    }

    private void CollectBlockingColliders()
    {
        if (blockingColliders != null &&
            blockingColliders.Length > 0)
        {
            return;
        }

        Collider[] foundColliders =
            GetComponentsInChildren<Collider>(true);

        List<Collider> validColliders =
            new();

        foreach (Collider foundCollider
                 in foundColliders)
        {
            if (foundCollider == null ||
                foundCollider.isTrigger)
            {
                continue;
            }

            validColliders.Add(foundCollider);
        }

        blockingColliders =
            validColliders.ToArray();
    }

    private void PrepareFadeMaterials()
    {
        CollectWallRenderers();

        _fadeMaterials.Clear();

        foreach (Renderer wallRenderer
                 in wallRenderers)
        {
            if (wallRenderer == null)
                continue;

            Material[] materials =
                wallRenderer.materials;

            foreach (Material material
                     in materials)
            {
                if (material == null ||
                    _fadeMaterials.Contains(material))
                {
                    continue;
                }

                _fadeMaterials.Add(material);
            }
        }
    }

    private static Color GetMaterialColor(
        Material material)
    {
        if (material.HasProperty("_BaseColor"))
        {
            return material.GetColor("_BaseColor");
        }

        if (material.HasProperty("_Color"))
        {
            return material.GetColor("_Color");
        }

        return Color.white;
    }

    private static void SetMaterialColor(
        Material material,
        Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private void SetWallAlpha(float alpha)
    {
        float clampedAlpha =
            Mathf.Clamp01(alpha);

        foreach (Material material
                 in _fadeMaterials)
        {
            if (material == null)
                continue;

            Color color =
                GetMaterialColor(material);

            color.a = clampedAlpha;

            SetMaterialColor(
                material,
                color
            );
        }
    }

    private void SetRenderersEnabled(bool enabled)
    {
        foreach (Renderer wallRenderer
                 in wallRenderers)
        {
            if (wallRenderer != null)
            {
                wallRenderer.enabled = enabled;
            }
        }
    }

    [Server]
    private void SetBlockingCollidersEnabled(bool enabled)
    {
        foreach (Collider wallCollider
                 in blockingColliders)
        {
            if (wallCollider != null)
            {
                wallCollider.enabled = enabled;
            }
        }
    }

    private void OnDestroy()
    {
        foreach (Material material
                 in _fadeMaterials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        _fadeMaterials.Clear();
    }
}