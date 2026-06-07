using UnityEngine;
using System;

public class BubbleBehaviorManager : MonoBehaviour
{
    //fly, clean, kick, burn
    public bool[] BubbleBools = {false, false, false, false};
    public bool Activated = false;
    private Action[] BubbleActions;
    private Rigidbody rb;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

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
            if (BubbleBools[index] == true && Activated)
            {
                BubbleActions[index]();
                Debug.Log("Bubble Action performed");
            }
        }
    }

    void flyBubble()
    {
        Debug.Log("flyBubble Activated");
        rb.isKinematic = false;
        rb.AddForce(Vector3.up * 5F, ForceMode.Force);
    }

    void cleanBubble()
    {
        
    }

    void kickBubble()
    {
        Debug.Log("kickBubble Activated");
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    void burnBubble()
    {
        
    }
}
