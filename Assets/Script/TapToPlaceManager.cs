using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class TapToPlaceManager : MonoBehaviour
{
    private const string PressActionPath = "TouchControls/Press";
    private const string PositionActionPath = "TouchControls/Position";

    [SerializeField] private Text debugText;
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private GameObject unActivated;
    [SerializeField] private GameObject mainPrefab;

    private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private PlayerInput playerInput;
    private InputAction pressAction;
    private InputAction positionAction;
    private MainSelectManager mainSelectManager;
    private bool handledCurrentPress;
    private bool firstHit = false;
    [SerializeField] private Transform arCamera;
    private Vector3 directionToCamera;

    public SessionManager sessionManager;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        mainSelectManager = GetComponent<MainSelectManager>();

        if (playerInput != null && playerInput.actions != null)
        {
            pressAction = playerInput.actions.FindAction(PressActionPath);
            positionAction = playerInput.actions.FindAction(PositionActionPath);
        }

        if (arRaycastManager == null)
        {
            arRaycastManager = FindFirstObjectByType<ARRaycastManager>();
        }
    }

    private void OnEnable()
    {
        if (pressAction == null || positionAction == null)
        {
            Debug.LogError(
                "TapToPlaceManager could not find the Press or Position Input Action.",
                this
            );
            return;
        }

        pressAction.performed += OnPressPerformed;
        pressAction.Enable();
        positionAction.Enable();
    }

    private void OnDisable()
    {
        if (pressAction == null)
        {
            return;
        }

        pressAction.performed -= OnPressPerformed;
    }

    private void Start()
    {
        SetDebugText("application started");
        directionToCamera.y = 0f;
    }

    private void Update()
    {
        if (!IsPointerPressed())
        {
            handledCurrentPress = false;
        }
    }

    private void OnPressPerformed(InputAction.CallbackContext context)
    {
        if (handledCurrentPress || positionAction == null)
        {
            return;
        }

        handledCurrentPress = true;
        TryPlaceAt(positionAction.ReadValue<Vector2>());
    }


    private void TryPlaceAt(Vector2 screenPosition)
    {
        if (arRaycastManager == null)
        {
            SetDebugText("missing ARRaycastManager");
            return;
        }

        if (firstHit)
        {
            return;
        }

        bool hasHit = arRaycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon);
        SetDebugText(hasHit.ToString());

        if (!hasHit)
        {
            return;
        }

        firstHit = true;

        Pose hitPose = hits[0].pose;

        if (unActivated != null)
        {
            unActivated.SetActive(false);
        }

        if (mainPrefab != null)
        {
            Vector3 directionToCamera = arCamera.position - hitPose.position;
            directionToCamera.y = 0f;

            Quaternion mainRotation = Quaternion.LookRotation(directionToCamera)*Quaternion.Euler(0f, 180f, 0f);

            GameObject spawned = Instantiate(mainPrefab, hitPose.position, mainRotation);
            BubbleClean spawnedBubbleClean = spawned.GetComponentInChildren<BubbleClean>(true);
            BubbleBloom spawnedBubbleBloom = spawned.GetComponentInChildren<BubbleBloom>(true);

            mainSelectManager?.SetBubbleClean(spawnedBubbleClean);
            PrefabAnimator animRef = spawned.GetComponentInChildren<PrefabAnimator>();
            sessionManager.bubbleAnimator = animRef.bubbleAnimator;
            sessionManager.robotAnimator = animRef.robotAnimator;

            mainSelectManager?.NotifyPrefabPlaced();
        }

    }

    private bool IsPointerPressed()
    {
        return pressAction != null && pressAction.IsPressed();
    }

    private void SetDebugText(string text)
    {
        if (debugText != null)
        {
            debugText.text = text;
        }
    }
}
