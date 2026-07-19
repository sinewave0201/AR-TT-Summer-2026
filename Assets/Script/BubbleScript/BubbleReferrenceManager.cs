using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class BubbleReferrenceManager : MonoBehaviour
{
    [SerializeField] private BubbleBehaviorManager bubbleBehaviorManager;

    [Header("Logic to choose interaction")]
    //used to select final interaction
    //0: fly away, 1: explode and clean, 2: grow, 3: kick
    public List<int> SelectWhichData = new List<int>();
    private int SelectIndex = 0;
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
        
        int HighestData = -1;
        for (int i = 0; i < SelectWhichData.Count; i++)
        {
            int CurData = SelectWhichData[i];
            int CurIndex = i;
            
            if (CurData > HighestData)
            {
                SelectIndex = CurIndex;
                HighestData = CurData;
            }
        }

        bubbleBehaviorManager.BubbleBehaviorSelect(SelectIndex);
    }

    //function used to increment dataSet
    public void AddToDataSet(int index)
    {
        Debug.Log("Added!");
        SelectWhichData[index] += 1;
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
