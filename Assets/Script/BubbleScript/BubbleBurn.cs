using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.Rendering;

[RequireComponent(typeof(Rigidbody))]
public class BubbleBurn : MonoBehaviour
{
    [Header("Burn")]
    [SerializeField] private string fireplaceTag = "Fireplace";
    [SerializeField] private GameObject healEffect;
    [SerializeField] private GameObject bubbleRootToDisable;

    [Header("Kick")]
    [SerializeField] private float forceMultiplier = 12f;
    [SerializeField] private float maxDragDistance = 2f;
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private LayerMask hitLayers = Physics.DefaultRaycastLayers;

    [Header("Guide Line")]
    [SerializeField] private LineRenderer guideLine;
    [SerializeField] private float guideLineWidth = 0.01f;
    [SerializeField] private Color guideLineColor = Color.white;
    [SerializeField, Range(0f, 1f)] private float guideLineAlpha = 0.7f;
    [SerializeField] private float guideDashLength = 0.04f;
    [SerializeField] private float guideGapLength = 0.05f;
    [SerializeField] private bool guideLineAlwaysOnTop = true;

    [Header("Motion Stop")]
    [SerializeField] private float kickLinearDamping = 3f;
    [SerializeField] private float kickAngularDamping = 3f;
    [SerializeField] private float stopSpeed = 0.04f;
    [SerializeField] private float stopAngularSpeed = 0.2f;
    [SerializeField] private float stopDelay = 0.35f;

    private Rigidbody rb;
    private bool burnEnabled;
    private bool burned;
    private bool kickEnabled;
    private bool dragging;
    private bool hasOriginalTransform;
    private float originalLinearDamping;
    private float originalAngularDamping;
    private float stoppedTime;
    private Vector2 lastScreenPosition;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private AudioSource audioSource;
    private BubbleResetPositionButton resetButtonController;
    private readonly List<LineRenderer> guideDashes = new List<LineRenderer>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        originalLinearDamping = rb.linearDamping;
        originalAngularDamping = rb.angularDamping;
        CaptureOriginalTransform();
        EnsureGuideLine();
        HideGuideLine();
        SetResetButton(false);

        if (bubbleRootToDisable == null)
        {
            bubbleRootToDisable = gameObject;
        }

        if (healEffect == null)
        {
            Transform foundEffect = FindChildContaining(transform, "heal");
            if (foundEffect != null)
            {
                healEffect = foundEffect.gameObject;
            }
        }
    }

    private void Update()
    {
        if (!kickEnabled)
        {
            return;
        }

        bool pressed = TryGetPointer(out Vector2 screenPosition);

        if (pressed && !dragging && PointerHitsBubble(screenPosition))
        {
            lastScreenPosition = screenPosition;
            dragging = true;
            UpdateGuideLine(screenPosition);
        }
        else if (pressed && dragging)
        {
            lastScreenPosition = screenPosition;
            UpdateGuideLine(screenPosition);
        }
        else if (!pressed && dragging)
        {
            ReleaseKick(lastScreenPosition);
            if (audioSource != null)
            {
                audioSource.Play();
            }
        }
    }

    private void FixedUpdate()
    {
        if (!kickEnabled || dragging || rb.isKinematic)
        {
            return;
        }

        ApplyAutoStop();
    }

    public void EnableKickInteraction()
    {
        CaptureOriginalTransform(true);
        kickEnabled = true;
        SetResetButton(true);

        SetKickDamping();
        stoppedTime = 0f;
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    public void DisableKickInteraction()
    {
        kickEnabled = false;
        dragging = false;
        stoppedTime = 0f;
        HideGuideLine();
        SetResetButton(false);
        RestoreOriginalDamping();
    }

    public void EnableBurn()
    {
        burnEnabled = true;
        burned = false;
    }

    public void DisableBurn()
    {
        burnEnabled = false;
    }

    public void ResetBurnState()
    {
        burned = false;
    }

    public void ResetBubblePosition()
    {
        if (!hasOriginalTransform)
        {
            CaptureOriginalTransform();
        }

        gameObject.SetActive(true);
        if (bubbleRootToDisable != null)
        {
            bubbleRootToDisable.SetActive(true);
        }

        transform.SetPositionAndRotation(originalPosition, originalRotation);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        RestoreOriginalDamping();
        rb.useGravity = false;
        rb.isKinematic = true;
        dragging = false;
        stoppedTime = 0f;
        burned = false;
        HideGuideLine();
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryBurn(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryBurn(other.gameObject);
    }

    private void TryBurn(GameObject other)
    {
        if (!burnEnabled || burned || !IsFireplace(other))
        {
            return;
        }

        burned = true;
        PlayHealEffect();
        bubbleRootToDisable.SetActive(false);

        AudioSource fireplaceAudio = other.GetComponent<AudioSource>();
        if (fireplaceAudio != null)
        {
            fireplaceAudio.Play();
        }
    }

    private bool IsFireplace(GameObject other)
    {
        bool tagMatches = !string.IsNullOrEmpty(fireplaceTag) && other.tag == fireplaceTag;

        return tagMatches
            || other.name.ToLowerInvariant().Contains("fireplace");
    }

    private void PlayHealEffect()
    {
        if (healEffect == null)
        {
            return;
        }

        GameObject effectInstance = Instantiate(healEffect, transform.position, healEffect.transform.rotation);
        effectInstance.SetActive(true);

        ParticleSystem[] particles = effectInstance.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem particle in particles)
        {
            particle.Play();
        }
    }

    private void CaptureOriginalTransform(bool force = false)
    {
        if (hasOriginalTransform && !force)
        {
            return;
        }

        originalPosition = transform.position;
        originalRotation = transform.rotation;
        hasOriginalTransform = true;
    }

    private bool TryGetPointer(out Vector2 screenPosition)
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        screenPosition = default;
        return false;
    }

    private bool PointerHitsBubble(Vector2 screenPosition)
    {
        Camera cam = GetCamera();
        if (cam == null)
        {
            return false;
        }

        Ray ray = cam.ScreenPointToRay(screenPosition);
        return Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, hitLayers)
            && hit.transform.IsChildOf(transform);
    }

    private void UpdateGuideLine(Vector2 screenPosition)
    {
        Vector3 endPoint = GetDragWorldPoint(screenPosition);
        DrawDashedGuideLine(transform.position, endPoint);
    }

    private void ReleaseKick(Vector2 screenPosition)
    {
        dragging = false;

        Vector3 dragVector = GetDragWorldPoint(screenPosition) - transform.position;
        HideGuideLine();

        float dragDistance = Mathf.Min(dragVector.magnitude, maxDragDistance);
        if (dragDistance <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 kickDirection = -dragVector.normalized;
        SetKickDamping();
        stoppedTime = 0f;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.AddForce(kickDirection * dragDistance * forceMultiplier, ForceMode.Impulse);
    }

    private void ApplyAutoStop()
    {
        bool movingSlowly = rb.linearVelocity.sqrMagnitude <= stopSpeed * stopSpeed
            && rb.angularVelocity.magnitude <= stopAngularSpeed;

        if (!movingSlowly)
        {
            stoppedTime = 0f;
            return;
        }

        stoppedTime += Time.fixedDeltaTime;
        if (stoppedTime < stopDelay)
        {
            return;
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true;
        stoppedTime = 0f;
    }

    private Vector3 GetDragWorldPoint(Vector2 screenPosition)
    {
        Camera cam = GetCamera();
        if (cam == null)
        {
            return transform.position;
        }

        Ray ray = cam.ScreenPointToRay(screenPosition);
        Plane dragPlane = new Plane(-cam.transform.forward, transform.position);

        if (!dragPlane.Raycast(ray, out float enter))
        {
            return transform.position;
        }

        Vector3 point = ray.GetPoint(enter);
        Vector3 offset = Vector3.ClampMagnitude(point - transform.position, maxDragDistance);
        return transform.position + offset;
    }

    private Camera GetCamera()
    {
        if (interactionCamera != null)
        {
            return interactionCamera;
        }

        interactionCamera = Camera.main;
        return interactionCamera;
    }

    private void EnsureGuideLine()
    {
        if (guideLine == null)
        {
            guideLine = gameObject.AddComponent<LineRenderer>();
        }

        guideLine.positionCount = 2;
        guideLine.useWorldSpace = true;
        guideLine.startWidth = guideLineWidth;
        guideLine.endWidth = guideLineWidth;
        guideLine.startColor = GetGuideLineColor();
        guideLine.endColor = GetGuideLineColor();
        guideLine.sortingOrder = short.MaxValue;

        guideLine.sharedMaterial = CreateGuideLineMaterial();

        guideLine.enabled = false;
        guideDashes.Add(guideLine);
    }

    private void DrawDashedGuideLine(Vector3 startPoint, Vector3 endPoint)
    {
        Vector3 line = endPoint - startPoint;
        float lineLength = line.magnitude;
        if (lineLength <= Mathf.Epsilon)
        {
            HideGuideLine();
            return;
        }

        Vector3 direction = line / lineLength;
        float dashLength = Mathf.Max(guideDashLength, 0.001f);
        float gapLength = Mathf.Max(guideGapLength, 0f);
        float stepLength = dashLength + gapLength;
        int dashCount = Mathf.Max(1, Mathf.CeilToInt(lineLength / stepLength));

        EnsureGuideDashCount(dashCount);

        for (int i = 0; i < guideDashes.Count; i++)
        {
            LineRenderer dash = guideDashes[i];
            bool active = i < dashCount;
            dash.enabled = active;

            if (!active)
            {
                continue;
            }

            float dashStartDistance = i * stepLength;
            float dashEndDistance = Mathf.Min(dashStartDistance + dashLength, lineLength);
            dash.SetPosition(0, startPoint + direction * dashStartDistance);
            dash.SetPosition(1, startPoint + direction * dashEndDistance);
        }
    }

    private void EnsureGuideDashCount(int dashCount)
    {
        while (guideDashes.Count < dashCount)
        {
            LineRenderer dash = CreateGuideDash(guideDashes.Count);
            guideDashes.Add(dash);
        }
    }

    private LineRenderer CreateGuideDash(int index)
    {
        GameObject dashObject = new GameObject($"Guide Line Dash {index + 1}");
        dashObject.transform.SetParent(transform, false);

        LineRenderer dash = dashObject.AddComponent<LineRenderer>();
        ApplyGuideLineSettings(dash);
        dash.enabled = false;
        return dash;
    }

    private void ApplyGuideLineSettings(LineRenderer line)
    {
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = guideLineWidth;
        line.endWidth = guideLineWidth;
        line.startColor = GetGuideLineColor();
        line.endColor = GetGuideLineColor();
        line.sortingOrder = short.MaxValue;

        if (guideLine != null && guideLine.sharedMaterial != null)
        {
            line.sharedMaterial = guideLine.sharedMaterial;
        }
        else
        {
            line.sharedMaterial = CreateGuideLineMaterial();
        }
    }

    private Color GetGuideLineColor()
    {
        Color color = guideLineColor;
        color.a *= guideLineAlpha;
        return color;
    }

    private Material CreateGuideLineMaterial()
    {
        Shader shader = guideLineAlwaysOnTop
            ? Shader.Find("Hidden/Internal-Colored")
            : Shader.Find("Sprites/Default");

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        Material material = new Material(shader);

        material.color = GetGuideLineColor();

        if (guideLineAlwaysOnTop)
        {
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_Cull", (int)CullMode.Off);
            material.SetInt("_ZTest", (int)CompareFunction.Always);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)RenderQueue.Overlay;
        }

        return material;
    }

    private void HideGuideLine()
    {
        foreach (LineRenderer dash in guideDashes)
        {
            if (dash != null)
            {
                dash.enabled = false;
            }
        }
    }

    private void SetKickDamping()
    {
        rb.linearDamping = kickLinearDamping;
        rb.angularDamping = kickAngularDamping;
    }

    private void RestoreOriginalDamping()
    {
        rb.linearDamping = originalLinearDamping;
        rb.angularDamping = originalAngularDamping;
    }

    private void SetResetButton(bool visible)
    {
        if (resetButtonController == null)
        {
            resetButtonController = FindFirstObjectByType<BubbleResetPositionButton>(FindObjectsInactive.Include);
        }

        if (resetButtonController != null)
        {
            resetButtonController.SetVisible(visible);
        }
    }

    private Transform FindChildContaining(Transform root, string text)
    {
        string lowerText = text.ToLowerInvariant();

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child != root && child.name.ToLowerInvariant().Contains(lowerText))
            {
                return child;
            }
        }

        return null;
    }
}
