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

        bubbleBehaviorManager = GetComponent<BubbleBehaviorManager>();
        if (bubbleBehaviorManager == null)
        {
            bubbleBehaviorManager = GetComponentInParent<BubbleBehaviorManager>();
        }
        if (bubbleBehaviorManager == null)
        {
            bubbleBehaviorManager = GetComponentInChildren<BubbleBehaviorManager>();
        }
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
