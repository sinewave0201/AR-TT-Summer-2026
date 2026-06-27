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

        ResolveBubbleBehaviorManager();
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

    public void BubbleBehaviorEnd()
    {
        ResolveBubbleBehaviorManager();

        if (bubbleBehaviorManager == null)
        {
            Debug.LogError(
                "BubbleBehaviorActivated could not find a BubbleBehaviorManager.",
                this
            );
            return;
        }

        bubbleBehaviorManager.BubbleBehaviorEnd();
    }

    private void ResolveBubbleBehaviorManager()
    {
        if (bubbleBehaviorManager == null)
        {
            bubbleBehaviorManager =
                FindFirstObjectByType<BubbleBehaviorManager>(
                    FindObjectsInactive.Include
                );
        }
    }
}
