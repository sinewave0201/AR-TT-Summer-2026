using UnityEngine;

public class BubbleBehaviorActivated : MonoBehaviour
{
    public BubbleBehaviorManager bubbleBehaviorManager;

    void Awake()
    {
        if (bubbleBehaviorManager != null)
        {
            return;
        }

        bubbleBehaviorManager = FindFirstObjectByType<BubbleBehaviorManager>();
    }

    void Start()
    {
        if (bubbleBehaviorManager == null)
        {
            Debug.LogError("BubbleBehaviorActivated needs a BubbleBehaviorManager reference.", this);
            enabled = false;
            return;
        }

        bubbleBehaviorManager.Activated = true;
        Debug.Log("Bubble Activated");
    }
}
