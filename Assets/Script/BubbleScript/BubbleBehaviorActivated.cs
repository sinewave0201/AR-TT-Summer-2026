using UnityEngine;

public class BubbleBehaviorActivated : MonoBehaviour
{
    public BubbleBehaviorManager bubbleBehaviorManager;
    void Start()
    {
        bubbleBehaviorManager.Activated = true;
        Debug.Log("Bubble Activated");
    }
}
