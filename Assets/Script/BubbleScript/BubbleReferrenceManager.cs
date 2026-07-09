using UnityEngine;
using TMPro;

public class BubbleReferrenceManager : MonoBehaviour
{
    [SerializeField] private BubbleBehaviorManager bubbleBehaviorManager;
    public int index;
    public TMP_InputField tMP_InputField; 

    void Awake()
    {
        ResolveBubbleBehaviorManager();
    }

    // Update is called once per frame
    public void BubbleBehaviorSelect()
    {
        if (bubbleBehaviorManager == null)
        {
            Debug.Log("I cant find any behavior Manager!!");
            ResolveBubbleBehaviorManager();
        }

        if (bubbleBehaviorManager == null)
        {
            Debug.LogError("BubbleReferrenceManager could not find a BubbleBehaviorManager.", this);
            return;
        }

        bubbleBehaviorManager.BubbleBehaviorSelect(index);
    }

    public void FinishInput()
    {
        if (bubbleBehaviorManager == null)
        {
            ResolveBubbleBehaviorManager();
        }

        if (bubbleBehaviorManager == null)
        {
            Debug.LogError("BubbleReferrenceManager could not find a BubbleBehaviorManager.", this);
            return;
        }

        bubbleBehaviorManager.FinishInput(tMP_InputField.text);
    }

    private void ResolveBubbleBehaviorManager()
    {
        bubbleBehaviorManager =
            FindFirstObjectByType<BubbleBehaviorManager>(
                FindObjectsInactive.Include
            );
    }

}
