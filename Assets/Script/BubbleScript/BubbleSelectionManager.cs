using UnityEngine;

public class BubbleSelectionManager : MonoBehaviour
{
    private BubbleBehaviorManager bubbleBehaviorManager;
    public int index;
    void Start()
    {
        bubbleBehaviorManager = FindFirstObjectByType<BubbleBehaviorManager>();
    }

    // Update is called once per frame
    public void BubbleBehaviorSelect()
    {
        bubbleBehaviorManager.BubbleBehaviorSelect(index);
    }
}
