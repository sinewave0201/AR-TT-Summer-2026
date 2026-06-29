using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BubbleBloom : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] private GameObject pot;
    [SerializeField] private GameObject flower;
    [SerializeField] private GameObject wateringCan;
    [SerializeField] private GameObject wateringEffect;

    [Header("Animators")]
    [SerializeField] private Animator bubbleAnimator;
    [SerializeField] private Animator flowerAnimator;
    [SerializeField] private Animator wateringCanAnimator;
    private string bubbleBloomState = "Bloom";
    private string flowerBloomState = "Armature|Bloom";
    private string movingTrigger = "moving";
    private string movingState = "Moving";
    private string wateringParameter = "watering";
    private string wateringState = "watering";
    private string wateringIdleState = "idle";

    [Header("Watering gesture & InputSystem")]
    [SerializeField] private InputActionReference pressAction;
    [SerializeField] private InputActionReference positionAction;
    private float WaterCanPrevPointerX;
    private int WaterCanPrevDirection;
    private bool WaterCanWasPressed;

    [Header("Growth")]
    [SerializeField, Min(0.01f)] private float waterPerSecond = 1f;
    [SerializeField, Min(0.1f)] private float bloomStartsAt = 5f;
    [SerializeField, Min(0.1f)] private float maximumWaterScale = 10f;
    [SerializeField] private Vector3 grownFlowerLocalPosition =
        new Vector3(0.734f, 0.135f, -2.382f);

    [Header("Runtime state")]
    public float waterScale;
    public bool watering;
    public bool waterCanEnabled;

    private Vector3 flowerStartLocalPosition;
    private Vector3 wateringCanStartLocalPosition;
    private Quaternion wateringCanStartLocalRotation;
    private ParticleSystem[] wateringParticles =
        System.Array.Empty<ParticleSystem>();
    private Coroutine startRoutine;
    private float wateringUntil;
    private bool trackingGesture;
    private bool wateringVisualInitialized;
    private bool flowerBloomStarted;

    private void Awake()
    {
        ResolveReferences();

        if (flower != null)
        {
            flowerStartLocalPosition = flower.transform.localPosition;
        }

        if (wateringCan != null)
        {
            wateringCanStartLocalPosition =
                wateringCan.transform.localPosition;
            wateringCanStartLocalRotation =
                wateringCan.transform.localRotation;
        }

        ApplyWatering(false);
        ResetFlowerAnimation();
    }

    private void Start()
    {
        pot?.SetActive(false);
        flower?.SetActive(false);
    }

    private void Update()
    {
        DetactWatering();

        if (!watering)
        {
            SetFlowerAnimationPlaying(false);
            return;
        }

        KeepWateringAnimationLooping();

        waterScale = Mathf.Min(
            maximumWaterScale,
            waterScale + waterPerSecond * Time.deltaTime
        );

        float growth = Mathf.InverseLerp(
            0f,
            bloomStartsAt,
            waterScale
        );

        if (flower != null)
        {
            flower.transform.localPosition = Vector3.Lerp(
                flowerStartLocalPosition,
                grownFlowerLocalPosition,
                growth
            );
        }

        SetFlowerAnimationPlaying(waterScale >= bloomStartsAt);
    }

    public void StartBloom()
    {
        if (startRoutine != null)
        {
            StopCoroutine(startRoutine);
        }

        startRoutine = StartCoroutine(StartBloomSequence());
    }

    public void EndBloom()
    {
        if (startRoutine != null)
        {
            StopCoroutine(startRoutine);
            startRoutine = null;
        }

        waterCanEnabled = false;
        wateringUntil = 0f;
        waterScale = 0f;
        trackingGesture = false;
        ApplyWatering(false);

        ResetFlowerAnimation();
        if (flower != null)
        {
            flower.transform.localPosition = flowerStartLocalPosition;
            flower.SetActive(false);
        }

        pot?.SetActive(false);

        if (wateringCan != null)
        {
            wateringCan.transform.localPosition =
                wateringCanStartLocalPosition;
            wateringCan.transform.localRotation =
                wateringCanStartLocalRotation;
        }

        if (wateringCanAnimator != null)
        {
            wateringCanAnimator.Play(wateringIdleState, 0, 0f);
            wateringCanAnimator.Update(0f);
            wateringCanAnimator.speed = 0f;
        }
    }

    private IEnumerator StartBloomSequence()
    {
        waterCanEnabled = false;
        wateringUntil = 0f;
        waterScale = 0f;
        trackingGesture = false;
        ApplyWatering(false);

        pot?.SetActive(true);
        if (flower != null)
        {
            flower.SetActive(true);
            flower.transform.localPosition = flowerStartLocalPosition;
        }
        ResetFlowerAnimation();

        if (bubbleAnimator != null)
        {
            bubbleAnimator.Play(bubbleBloomState, 0, 0f);
            bubbleAnimator.Update(0f);
            yield return WaitForStateToFinish(
                bubbleAnimator,
                bubbleBloomState
            );
        }

        if (wateringCanAnimator != null)
        {
            //watering Can animator speed will be set to 0f on default
            wateringCanAnimator.speed = 1f;
            wateringCanAnimator.ResetTrigger(movingTrigger);
            wateringCanAnimator.SetTrigger(movingTrigger);
            yield return WaitForStateToFinish(
                wateringCanAnimator,
                movingState
            );
        }

        waterCanEnabled = true;
        startRoutine = null;
        Debug.Log(
            "Watering can moving animation finished; gesture enabled.",
            this
        );
    }

    #region input system functions to detact watering
    private void OnEnable()
    {
        pressAction?.action.Enable();
        positionAction?.action.Enable();
    }

    private void OnDisable()
    {
        pressAction?.action.Disable();
        positionAction?.action.Disable();

        WaterCanWasPressed = false;
        WaterCanPrevDirection = 0;
    }

    private void DetactWatering()
    {
        //if bloom is started
        if (!waterCanEnabled ||
        pressAction == null ||
        positionAction == null)
        {
            WaterCanWasPressed = false;
            WaterCanPrevDirection = 0;
            return;
        }

        //whether pressed or not
        bool pressed = pressAction.action.IsPressed();
        float pointerX = positionAction.action.ReadValue<Vector2>().x;


        if (!pressed)
        {
            WaterCanWasPressed = false;
            WaterCanPrevDirection = 0;
            ApplyWatering(false);
            return;
        }

        if (!WaterCanWasPressed)
        {
            WaterCanWasPressed = true;
            WaterCanPrevPointerX = pointerX;
            WaterCanPrevDirection = 0;
            return;
        }

        float deltaX = pointerX - WaterCanPrevPointerX;
        WaterCanPrevPointerX = pointerX;

        if (Mathf.Abs(deltaX) < 0.2f)
        {
            return;
        }

        int currentDirection = deltaX > 0f ? 1 : -1;

        if (WaterCanPrevDirection != 0 &&
            currentDirection != WaterCanPrevDirection)
        {
            Debug.Log($"Direction changed: {WaterCanPrevDirection} -> {currentDirection}");
            ApplyWatering(waterCanEnabled);
        }

        WaterCanPrevDirection = currentDirection;
    }
    #endregion

    private static IEnumerator WaitForStateToFinish(
        Animator target,
        string stateName)
    {
        const float timeoutSeconds = 10f;
        float elapsed = 0f;
        bool enteredState = false;

        while (target != null && elapsed < timeoutSeconds)
        {
            AnimatorStateInfo state =
                target.GetCurrentAnimatorStateInfo(0);

            if (state.IsName(stateName))
            {
                enteredState = true;
                if (!target.IsInTransition(0) &&
                    state.normalizedTime >= 1f)
                {
                    yield break;
                }
            }
            else if (enteredState)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!enteredState)
        {
            Debug.LogWarning(
                $"Animator never entered state '{stateName}'."
            );
        }
    }

    private void ApplyWatering(bool active)
    {
        if (wateringVisualInitialized && watering == active)
        {
            return;
        }

        wateringVisualInitialized = true;
        watering = active;

        if (wateringCanAnimator != null)
        {
            wateringCanAnimator.speed = active ? 1f : 0f;
            wateringCanAnimator.SetBool(wateringParameter, active);

            if (active)
            {
                wateringCanAnimator.Play(wateringState, 0, 0f);
            }
        }

        if (active && wateringEffect != null)
        {
            wateringEffect.SetActive(true);
        }

        foreach (ParticleSystem particle in wateringParticles)
        {
            if (particle == null)
            {
                continue;
            }

            ParticleSystem.MainModule main = particle.main;
            main.loop = active;

            if (active)
            {
                particle.gameObject.SetActive(true);
                ParticleSystem.EmissionModule emission =
                    particle.emission;
                emission.enabled = true;

                ParticleSystemRenderer particleRenderer =
                    particle.GetComponent<ParticleSystemRenderer>();
                if (particleRenderer != null)
                {
                    particleRenderer.enabled = true;
                }

                particle.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );
                particle.Clear(true);
                particle.Play(true);
            }
            else
            {
                particle.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmitting
                );
            }
        }

        if (active)
        {
            StartCoroutine(VerifyWateringParticles());
        }
    }

    private IEnumerator VerifyWateringParticles()
    {
        yield return null;
        yield return null;

        int aliveParticles = 0;
        foreach (ParticleSystem particle in wateringParticles)
        {
            if (particle != null)
            {
                aliveParticles += particle.particleCount;
            }
        }
    }

    private void KeepWateringAnimationLooping()
    {
        if (wateringCanAnimator == null)
        {
            return;
        }

        AnimatorStateInfo state =
            wateringCanAnimator.GetCurrentAnimatorStateInfo(0);

        if (!state.IsName(wateringState) ||
            state.normalizedTime >= 1f)
        {
            wateringCanAnimator.Play(wateringState, 0, 0f);
        }

        wateringCanAnimator.speed = 1f;
    }

    private void SetFlowerAnimationPlaying(bool active)
    {
        if (flowerAnimator == null)
        {
            return;
        }

        if (active && !flowerBloomStarted)
        {
            flowerAnimator.Play(flowerBloomState, 0, 0f);
            flowerBloomStarted = true;
        }

        if (active &&
            flowerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
        {
            flowerAnimator.Play(flowerBloomState, 0, 0f);
        }

        flowerAnimator.speed = active ? 1f : 0f;
    }

    private void ResetFlowerAnimation()
    {
        flowerBloomStarted = false;

        if (flowerAnimator == null)
        {
            return;
        }

        flowerAnimator.enabled = true;
        flowerAnimator.Play(flowerBloomState, 0, 0f);
        flowerAnimator.Update(0f);
        flowerAnimator.speed = 0f;
    }

    private void ResolveReferences()
    {
        pot ??= FindChild("flowerpot");
        flower ??= FindChild("flower");
        wateringCan ??= FindChild("watering can");

        if (flowerAnimator == null && flower != null)
        {
            flowerAnimator = flower.GetComponent<Animator>();
        }

        if (wateringCanAnimator == null && wateringCan != null)
        {
            wateringCanAnimator = wateringCan.GetComponent<Animator>();
        }

        GameObject effectRoot =
            wateringEffect != null ? wateringEffect : wateringCan;
        wateringParticles = effectRoot != null
            ? effectRoot.GetComponentsInChildren<ParticleSystem>(true)
            : System.Array.Empty<ParticleSystem>();
    }

    private GameObject FindChild(string objectName)
    {
        string target = objectName.ToLowerInvariant();
        Transform[] children =
            GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child != transform &&
                child.name.ToLowerInvariant() == target)
            {
                return child.gameObject;
            }
        }

        foreach (Transform child in children)
        {
            if (child != transform &&
                child.name.ToLowerInvariant().Contains(target))
            {
                return child.gameObject;
            }
        }

        return null;
    }
}
