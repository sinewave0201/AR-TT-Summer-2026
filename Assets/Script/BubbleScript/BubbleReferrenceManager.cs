using UnityEngine;
using TMPro;

public class BubbleReferrenceManager : MonoBehaviour
{
    [SerializeField] private BubbleBehaviorManager bubbleBehaviorManager;
    public int index;
    public TMP_InputField tMP_InputField; 

    void Awake()
    {
        bubbleBehaviorManager = FindFirstObjectByType<BubbleBehaviorManager>();
    }

    // Update is called once per frame
    public void BubbleBehaviorSelect()
    {
        if (bubbleBehaviorManager == null)
        {
            Debug.Log("I cant find any behavior Manager!!");
            bubbleBehaviorManager = FindFirstObjectByType<BubbleBehaviorManager>();
        }
        bubbleBehaviorManager.BubbleBehaviorSelect(index);
    }

    public void FinishInput()
    {
        if (bubbleBehaviorManager == null)
        {
            bubbleBehaviorManager = FindFirstObjectByType<BubbleBehaviorManager>();
        }
        bubbleBehaviorManager.FinishInput(tMP_InputField.text);
    }

}
