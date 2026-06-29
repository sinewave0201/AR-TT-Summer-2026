using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.VisualScripting;

public class MainSelectManager : MonoBehaviour
{
    private const string PressActionPath = "TouchControls/Press";
    private const string PositionActionPath = "TouchControls/Position";

    [Header("Player inputs")]
    private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private PlayerInput playerInput;
    private InputAction pressAction;
    private InputAction positionAction;
    private bool handledCurrentPress;
    private bool suppressSelectionUntilRelease;
    [SerializeField] private ARRaycastManager arRaycastManager;

    [Header("references")]

    public GameObject sessionManager;
    public GameObject vaultManager;
    public GameObject calendarManager;
    private AudioSource audioSource;
    public bool OpenUI = true;

    [Header ("BubbleClean")]
    [SerializeField] private BubbleClean bubbleClean;
    [SerializeField] private float broomSurfaceOffset = 0.01f;
    [SerializeField, Min(0.001f)] private float broomSmoothTime = 0.04f;
    private bool draggingBroom;
    private Transform draggedBroom;
    private float broomDragDistanceFromCamera;
    private Vector3 broomVelocity;
    private Vector3 broomLastSurfacePoint;
    private bool broomHasLastSurfacePoint;
    
    [Header("Sound Effects")]
    [SerializeField] private AudioSource broomSound;
    [SerializeField] private AudioSource waterSound; 
    
    #region Player input
    private void OnEnable()
    {
        if (pressAction == null || positionAction == null)
        {
            Debug.LogError("Press or Position Input Action was not found.", this);
            return;
        }

        pressAction.performed += OnPressPerformed;

        pressAction.Enable();
        positionAction.Enable();
    }

    private void OnDisable()
    {
        if (pressAction != null)
        {
            pressAction.performed -= OnPressPerformed;
        }
    }

    private void OnPressPerformed(InputAction.CallbackContext context)
    {
        if (handledCurrentPress || suppressSelectionUntilRelease)
        {
            return;
        }

        Vector2 screenPos = positionAction.ReadValue<Vector2>();
        MainSelectAction(screenPos);
    }
    #endregion
    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

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
    

    private void Update()
    {
        bool pressed = IsPointerPressed();

        if (!IsPointerPressed())
        {
            handledCurrentPress = false;
        }

        //dragBroom logic in bubble clean
        if (pressed && draggingBroom && draggedBroom != null)
        {
            DragBroom();
        }

        if (!pressed)
        {
            if (draggingBroom)
            {
                bubbleClean?.EndCleanStroke();

                if (broomSound != null)
                {
                    broomSound.Stop();
                    broomSound.loop = false;
                    broomSound = null;
                }
            }
            handledCurrentPress = false;
            draggingBroom = false;
            draggedBroom = null;
            broomVelocity = Vector3.zero;
            broomHasLastSurfacePoint = false;

            if (suppressSelectionUntilRelease)
            {
                suppressSelectionUntilRelease = false;
                OpenUI = false;
            }
        }
    }

    private bool IsPointerPressed()
    {
        return pressAction != null && pressAction.IsPressed();
    }

    private void MainSelectAction(Vector2 screenPos)
    {
        Debug.Log("touched!!");
        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            //only handle when ray was success
            handledCurrentPress = true;

            audioSource = hit.collider.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.loop = false;
                audioSource.Play();
            }

            if (hit.collider.CompareTag("Vault") && !OpenUI)
            {
                Debug.Log("touch vault!!");
                OpenUI = true;
                vaultManager.SetActive(true);
            }

            if (hit.collider.CompareTag("RobotSession") && !OpenUI)
            {
                Debug.Log("touch robot!!");
                OpenUI = true;
                sessionManager.SetActive(true);
            }

            if (hit.collider.CompareTag("Calendar") && !OpenUI)
            {
                Debug.Log("touch calendar!!");
                OpenUI = true;
                calendarManager.SetActive(true);
            }

            //used for bubble clean bubblem behavior
            if (hit.collider.CompareTag("Broom"))
            {
                Debug.Log("touch broom!!");
                if (bubbleClean != null && bubbleClean.BroomEnabled)
                {
                    broomSound = hit.collider.GetComponent<AudioSource>();
                    if (broomSound != null)
                    {
                        broomSound.loop = true;
                        broomSound.Play();
                    }

                    draggingBroom = true;
                    draggedBroom = hit.collider.transform;
                    broomVelocity = Vector3.zero;
                    broomHasLastSurfacePoint = false;
                    bubbleClean.EndCleanStroke();
                    broomDragDistanceFromCamera = Vector3.Distance(Camera.main.transform.position, draggedBroom.position);
                }
            }
        }
    }

    public void SetBubbleClean(BubbleClean spawnedBubbleClean)
    {
        bubbleClean = spawnedBubbleClean;
    }


    public void ResetBroomInteraction()
    {
        bubbleClean?.EndCleanStroke();
        bubbleClean?.ResetBubbleClean();

        if (broomSound != null)
        {
            broomSound.Stop();
            broomSound.loop = false;
            broomSound = null;
        }

        draggingBroom = false;
        draggedBroom = null;
        broomVelocity = Vector3.zero;
        broomHasLastSurfacePoint = false;
    }

    public void NotifyPrefabPlaced()
    {
        // The placement press must not also select a collider on the new prefab.
        suppressSelectionUntilRelease = true;
        handledCurrentPress = true;
        OpenUI = true;
    }

    public void CloseVault()
    {
        vaultManager.SetActive(false);
        OpenUI = false;
    }

    public void CloseSession()
    {
        sessionManager.SetActive(false);
        OpenUI = false;
    }

    public void CloseCalendar()
    {
        calendarManager.SetActive(false);
        OpenUI = false;
    }


    public void BeginSession()
    {
        sessionManager.SetActive(true);
        sessionManager.GetComponent<SessionManager>().BeginSession();
    }

    //broom logic
    private void DragBroom()
    {
        Vector2 screenPos = GetPointerPosition();
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        int cleanableLayer = LayerMask.NameToLayer("Cleanable");

        if (cleanableLayer < 0)
        {
            Debug.LogError("The Cleanable layer is missing.");
            return;
        }

        int cleanableMask = 1 << cleanableLayer;
        RaycastHit[] surfaceHits = Physics.RaycastAll(
            ray,
            Mathf.Infinity,
            cleanableMask,
            QueryTriggerInteraction.Ignore
        );

        if (surfaceHits.Length > 0)
        {
            RaycastHit surfaceHit = SelectContinuousSurfaceHit(surfaceHits);
            Vector3 targetPosition =
                surfaceHit.point + surfaceHit.normal * broomSurfaceOffset;

            draggedBroom.position = Vector3.SmoothDamp(
                draggedBroom.position,
                targetPosition,
                ref broomVelocity,
                broomSmoothTime
            );

            broomLastSurfacePoint = surfaceHit.point;
            broomHasLastSurfacePoint = true;
            bubbleClean?.CleanAt(surfaceHit);
        }
    }

    private RaycastHit SelectContinuousSurfaceHit(RaycastHit[] surfaceHits)
    {
        RaycastHit bestHit = surfaceHits[0];
        float bestScore = float.PositiveInfinity;
        bool hasPaintableHit = false;
        bool hasMeshColliderHit = false;

        foreach (RaycastHit candidate in surfaceHits)
        {
            if (bubbleClean != null &&
                bubbleClean.CanClean(candidate.collider))
            {
                hasPaintableHit = true;
            }

            if (candidate.collider is MeshCollider)
            {
                hasMeshColliderHit = true;
            }
        }

        foreach (RaycastHit candidate in surfaceHits)
        {
            if (hasPaintableHit &&
                !bubbleClean.CanClean(candidate.collider))
            {
                continue;
            }

            if (!hasPaintableHit &&
                hasMeshColliderHit &&
                candidate.collider is not MeshCollider)
            {
                continue;
            }

            float score = broomHasLastSurfacePoint
                ? Vector3.SqrMagnitude(candidate.point - broomLastSurfacePoint)
                : candidate.distance;

            if (score < bestScore)
            {
                bestScore = score;
                bestHit = candidate;
            }
        }

        return bestHit;
    }

    private Vector2 GetPointerPosition()
    {
        return positionAction != null
            ? positionAction.ReadValue<Vector2>()
            : Vector2.zero;
    }
}
