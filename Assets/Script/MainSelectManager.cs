using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.VisualScripting;

public class MainSelectManager : MonoBehaviour
{
    private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private PlayerInput playerInput;
    private InputAction touchAction;
    private bool handledCurrentPress;
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private string touchActionName = "Touch";

    public GameObject sessionManager;
    public GameObject vaultManager;
    public GameObject calendarManager;
    private AudioSource audioSource;
    private bool OpenUI = false;

    [Header ("BubbleClean")]
    [SerializeField] private BubbleClean bubbleClean;
    [SerializeField] private float broomSurfaceOffset = 0.01f;
    [SerializeField, Min(0.001f)] private float broomSmoothTime = 0.04f;
    private bool draggingBroom;
    private Transform draggedBroom;
    private float dragDistanceFromCamera;
    private Vector3 broomVelocity;
    private Vector3 lastSurfacePoint;
    private bool hasLastSurfacePoint;
        
    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        touchAction = playerInput != null && playerInput.actions != null
            ? playerInput.actions.FindAction(touchActionName)
            : null;

        if (arRaycastManager == null)
        {
            arRaycastManager = FindFirstObjectByType<ARRaycastManager>();
        }
    }
    
    private void OnEnable()
    {
        if (touchAction == null)
        {
            return;
        }

        touchAction.performed += OnTouchPerformed;
        touchAction.Enable();
    }

    private void OnDisable()
    {
        if (touchAction == null)
        {
            return;
        }

        touchAction.performed -= OnTouchPerformed;
    }

    private void Update()
    {
        bool pressed = IsPointerPressed();

        if (!IsPointerPressed())
        {
            handledCurrentPress = false;
        }

        if (pressed && draggingBroom && draggedBroom != null)
        {
            DragBroom();
        }


        if (!pressed)
        {
            if (draggingBroom)
            {
                bubbleClean?.EndCleanStroke();
            }

            handledCurrentPress = false;
            draggingBroom = false;
            draggedBroom = null;
            broomVelocity = Vector3.zero;
            hasLastSurfacePoint = false;
        }
    }

    private void OnTouchPerformed(InputAction.CallbackContext context)
    {
        if (handledCurrentPress)
        {
            return;
        }

        handledCurrentPress = true;
        MainSelectAction(context.ReadValue<Vector2>());
    }

    private bool IsPointerPressed()
    {
        bool touchPressed = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.isPressed;
        return touchPressed || mousePressed;
    }

    private void MainSelectAction(Vector2 screenPos)
    {
        Debug.Log("touched!!");
        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            audioSource = hit.collider.GetComponent<AudioSource>();
            if (audioSource != null)
            {
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

            if (hit.collider.CompareTag("Broom"))
            {
                Debug.Log("touch broom!!");
                if (bubbleClean != null && bubbleClean.BroomEnabled)
                {
                    draggingBroom = true;
                    draggedBroom = hit.collider.transform;
                    broomVelocity = Vector3.zero;
                    hasLastSurfacePoint = false;
                    bubbleClean.EndCleanStroke();
                    dragDistanceFromCamera = Vector3.Distance(Camera.main.transform.position, draggedBroom.position);
                }
            }
        }
    }

    public void SetBubbleClean(BubbleClean spawnedBubbleClean)
    {
        bubbleClean = spawnedBubbleClean;
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

            lastSurfacePoint = surfaceHit.point;
            hasLastSurfacePoint = true;
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

            float score = hasLastSurfacePoint
                ? Vector3.SqrMagnitude(candidate.point - lastSurfacePoint)
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
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            return Mouse.current.position.ReadValue();
        }

        return Vector2.zero;
    }
}
