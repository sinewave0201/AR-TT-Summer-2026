using UnityEngine;
using TMPro;

public class BubbleReferrenceManager : MonoBehaviour
{
    private BubbleBehaviorManager bubbleBehaviorManager;
    public int index;
    public TMP_InputField tMP_InputField; 
    void Start()
    {
        bubbleBehaviorManager = FindFirstObjectByType<BubbleBehaviorManager>();
    }

    // Update is called once per frame
    public void BubbleBehaviorSelect()
    {
        bubbleBehaviorManager.BubbleBehaviorSelect(index);
    }

    public void FinishInput()
    {
        bubbleBehaviorManager.FinishInput(tMP_InputField.text);
    }
}
