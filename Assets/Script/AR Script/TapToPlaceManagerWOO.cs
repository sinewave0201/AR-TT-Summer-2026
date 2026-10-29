using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class TapToPlaceManagerWOO : MonoBehaviour, IPrefabInitiationSource
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
    private bool firstHit;
    private bool prefabInitiated;
    public bool IsPrefabInitiated => prefabInitiated;
    [SerializeField] private Transform arCamera;

    [Header("Networking")]
    [SerializeField] private NetworkObject robotControlBridgePrefab;

    private NetworkObject spawnedControlBridge;

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

        if (AvatarPrefab == null)
        {
            SetDebugText("missing AvatarPrefab");
            firstHit = false;
            return;
        }

        Quaternion avatarRotation = hitPose.rotation;
        if (arCamera != null)
        {
            Vector3 directionToCamera = arCamera.position - hitPose.position;
            directionToCamera.y = 0f;

            if (directionToCamera.sqrMagnitude > 0.0001f)
            {
                avatarRotation = Quaternion.LookRotation(directionToCamera);
            }
        }

        GameObject spawnedAvatar = Instantiate(
            AvatarPrefab,
            hitPose.position,
            avatarRotation);

        mainSelectManager?.NotifyPrefabPlaced();
        prefabInitiated = true;
        AnimationManager animationManager =
            spawnedAvatar.GetComponentInChildren<AnimationManager>(true);
        SoundManager soundManager =
            spawnedAvatar.GetComponentInChildren<SoundManager>(true);
        StartHostAndSpawnControlBridge(animationManager, soundManager);
    }

    private void StartHostAndSpawnControlBridge(
        AnimationManager animationManager,
        SoundManager soundManager)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            Debug.LogError("Cannot start Host because no NetworkManager exists in the scene.", this);
            SetDebugText("missing NetworkManager");
            return;
        }

        if (!networkManager.IsListening && !networkManager.StartHost())
        {
            Debug.LogError("NetworkManager failed to start as Host.", this);
            SetDebugText("failed to start Host");
            return;
        }

        if (!networkManager.IsServer)
        {
            Debug.LogError("Robot was placed on a non-Host instance.", this);
            SetDebugText("not running as Host");
            return;
        }

        if (robotControlBridgePrefab == null)
        {
            Debug.LogError("RobotControlBridge Prefab is not assigned.", this);
            SetDebugText("missing RobotControlBridge");
            return;
        }

        if (spawnedControlBridge == null)
        {
            spawnedControlBridge = Instantiate(robotControlBridgePrefab);

            InheritNetworkBehavior bridge =
                spawnedControlBridge.GetComponent<InheritNetworkBehavior>();
            if (bridge == null)
            {
                Debug.LogError(
                    "RobotControlBridge Prefab is missing InheritNetworkBehavior.",
                    this);
                Destroy(spawnedControlBridge.gameObject);
                spawnedControlBridge = null;
                SetDebugText("missing bridge behaviour");
                return;
            }

            if (animationManager == null || soundManager == null)
            {
                Debug.LogError(
                    "The spawned Robot needs AnimationManager and SoundManager.",
                    this);
                Destroy(spawnedControlBridge.gameObject);
                spawnedControlBridge = null;
                SetDebugText("missing Robot managers");
                return;
            }

            bridge.Initialize(animationManager, soundManager);
            spawnedControlBridge.Spawn(destroyWithScene: false);
        }

        SetDebugText("Host started");
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
