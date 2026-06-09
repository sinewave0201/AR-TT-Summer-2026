using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class TapToPlaceManager : MonoBehaviour
{
    [SerializeField] private Text debugText;
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private GameObject unActivated;
    [SerializeField] private GameObject mainManager;
    [SerializeField] private GameObject robotPrefab;
    [SerializeField] private GameObject thoughtBubblePrefab;
    [SerializeField] private string touchActionName = "Touch";

    private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private PlayerInput playerInput;
    private InputAction touchAction;
    private bool handledCurrentPress;
    private bool firstHit = false;

    private Animator robotAnimator;
    private Animator bubbleAnimator;
    private Transform robotTransform;

    [SerializeField] private Transform arCamera;
    private Vector3 directionToCamera;
    private Quaternion robotRotation;

    private void Awake()
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

    private void OnTouchPerformed(InputAction.CallbackContext context)
    {
        if (handledCurrentPress)
        {
            return;
        }

        handledCurrentPress = true;
        TryPlaceAt(context.ReadValue<Vector2>());
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

        if (robotPrefab != null)
        {
            Vector3 directionToCamera = arCamera.position - hitPose.position;
            directionToCamera.y = 0f;

            Quaternion robotRotation = Quaternion.LookRotation(directionToCamera);

            GameObject robot = Instantiate(robotPrefab, hitPose.position, robotRotation);

            robotAnimator = robot.GetComponent<Animator>();
            robotTransform = robot.transform;
        }

        if (thoughtBubblePrefab != null)
        {
            Vector3 bubblePosition = robotTransform.position + Vector3.up * 0.2f - robotTransform.right * 0.2f;


            GameObject bubble = Instantiate(thoughtBubblePrefab, bubblePosition, hitPose.rotation);
            bubbleAnimator = bubble.GetComponent<Animator>();
        }

        if (mainManager != null)
        {
            mainManager.SetActive(true);
            MainManager manager = mainManager.GetComponent<MainManager>();
            manager.robotAnimator = robotAnimator;
            manager.bubbleAnimator = bubbleAnimator;
        }
    }

    private bool IsPointerPressed()
    {
        bool touchPressed = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.isPressed;
        return touchPressed || mousePressed;
    }

    private void SetDebugText(string text)
    {
        if (debugText != null)
        {
            debugText.text = text;
        }
    }
}
