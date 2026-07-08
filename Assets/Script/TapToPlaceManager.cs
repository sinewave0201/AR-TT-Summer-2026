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
    [SerializeField] private GameObject AvatarPrefab;

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

    [Header("Change Avatar Logic")]
    public ChangeAvatarScript changeAvatarScript;

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

            //spawn in avatar and main prefab
            GameObject spawnedMain = Instantiate(mainPrefab, hitPose.position, mainRotation);
            Vector3 avatarOffset = mainRotation * new Vector3(1.87f * 0.15f, 0.059f * 0.15f, -2.239f * 0.15f);
            Vector3 avatarPosition = hitPose.position + avatarOffset;
            GameObject spawnedAvatar = null;

            if (AvatarPrefab != null)
            {
                spawnedAvatar = Instantiate(AvatarPrefab, avatarPosition, mainRotation);
            }
            
            BubbleClean spawnedBubbleClean = spawnedMain.GetComponentInChildren<BubbleClean>(true);

            mainSelectManager?.SetBubbleClean(spawnedBubbleClean);
            PrefabAnimator animRef = spawnedMain.GetComponentInChildren<PrefabAnimator>();
            
            //pass the animator into session Manager
            if (spawnedAvatar != null)
            {
                sessionManager.robotAnimator = spawnedAvatar.GetComponentInChildren<Animator>(true);
            }

            sessionManager.bubbleAnimator = animRef.bubbleAnimator;

            //pass the current avatar reference to change avatar script
            if (changeAvatarScript != null && spawnedAvatar != null)
            {
                changeAvatarScript.SetCurrentAvatar(
                    spawnedAvatar,
                    AvatarPrefab,
                    avatarPosition,
                    mainRotation,
                    sessionManager);
                changeAvatarScript.prefabPlaced = true;
            }

            
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
