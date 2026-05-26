using UnityEngine;
using UnityEngine.InputSystem;

public class RaycastSelect : MonoBehaviour
{
    public Camera arCamera;
    [SerializeField]private GameObject clickedObject;
    [SerializeField]private Vector2 screenPosition;

    void Awake()
    {
        if (arCamera == null)
        {
            arCamera = Camera.main;
        }
    }

    private bool TryGetPressPosition(out Vector2 screenPosition)
    {
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                screenPosition = touch.position.ReadValue();
                return true;
            }
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        screenPosition = default;
        return false;
    }


    void Update()
    {
        if (arCamera == null) return;
        if (!TryGetPressPosition(out screenPosition)) return;
        
        Ray ray = arCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (clickedObject != null && clickedObject == hit.collider.gameObject){
                return;
            }
            else
            {
                DisableRotation(clickedObject);
                clickedObject = hit.collider.gameObject;

                Debug.Log("i hit something new");

                EnableRotation(clickedObject);
            }
                
        }
    }

    private void EnableRotation(GameObject gameObject)
    {
        if (gameObject == null) return;

        SceneTemplate_RotateCube rotate = gameObject.GetComponent<SceneTemplate_RotateCube>();

        if (rotate != null)
        {
            rotate.enabled = true;
        }
    }

    private void DisableRotation(GameObject gameObject)
    {
        if (gameObject == null) return;
        
        SceneTemplate_RotateCube rotate = gameObject.GetComponent<SceneTemplate_RotateCube>();

        if (rotate != null)
        {
            rotate.enabled = false;
        }
    }
}
