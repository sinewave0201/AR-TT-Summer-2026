using UnityEngine;
using System;

public class BubbleBehaviorManager : MonoBehaviour
{
    //fly, clean, kick, burn
    public bool[] BubbleBools = {false, false, false, false};
    private Action[] BubbleActions;
    void Awake()
    {
        BubbleActions = new Action[]
        {
            flyBubble,
            cleanBubble,
            kickBubble,
            burnBubble
        };
    }

    public void BubbleBehaviorSelect(int index)
    {
        BubbleBools[index] = true;
    }


    void Update()
    {
        for (int index = 0; index < BubbleBools.Length; index++)
        {
            if (BubbleBools[index] == true)
            {
                BubbleActions[index]();
            }
        }
    }

    void flyBubble()
    {
        
    }

    void cleanBubble()
    {
        
    }

    void kickBubble()
    {
        
    }

    void burnBubble()
    {
        
    }
}
