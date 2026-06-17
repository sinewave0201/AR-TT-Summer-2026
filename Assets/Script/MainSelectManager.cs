using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

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
            if (hit.collider.CompareTag("Vault"))
            {
                Debug.Log("touch vault!!");
                vaultManager.SetActive(true);
            }

            if (hit.collider.CompareTag("RobotSession"))
            {
                Debug.Log("touch robot!!");
                sessionManager.SetActive(true);
            }

            if (hit.collider.CompareTag("Calendar"))
            {
                Debug.Log("touch calendar!!");
                calendarManager.SetActive(true);
            }
        }
    }

    public void CloseVault()
    {
        vaultManager.SetActive(false);
    }

    public void CloseSession()
    {
        sessionManager.SetActive(false);
    }

    public void CloseCalendar()
    {
        calendarManager.SetActive(false);
    }


    public void BeginSession()
    {
        sessionManager.SetActive(true);
        sessionManager.GetComponent<SessionManager>().BeginSession();
    }
}
