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

    void OnEnable()
    {
        ResolveBubbleBehaviorManager();

        if (bubbleBehaviorManager == null)
        {
            Debug.LogError("BubbleBehaviorActivated needs a BubbleBehaviorManager reference.", this);
            enabled = false;
            return;
        }

        // bubbleBehaviorManager.gameObject.SetActive(true);
        bubbleBehaviorManager.enabled = true;
        bubbleBehaviorManager.BubbleBehaviorActivate();
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
