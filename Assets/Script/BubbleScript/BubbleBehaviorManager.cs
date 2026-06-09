using UnityEngine;
using System;
using TMPro;

public class BubbleBehaviorManager : MonoBehaviour
{
    //fly, clean, kick, burn
    public bool[] BubbleBools = {false, false, false, false};
    public bool Activated = false;
    private Action[] BubbleActions;
    private Rigidbody rb;
    public Animator animator;
    public TMP_Text bubbleText;

    public void FinishInput(string content)
    {
        bubbleText.text = content;
    }

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("BubbleBehaviorManager needs a Rigidbody on the same GameObject.", this);
            enabled = false;
            return;
        }

        if (animator == null)
        {
            Debug.LogWarning("BubbleBehaviorManager did not find an Animator on the same GameObject.", this);
        }

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
        if (index < 0 || index >= BubbleBools.Length)
        {
            Debug.LogError($"Bubble behavior index {index} is out of range.", this);
            return;
        }

        BubbleBools[index] = true;
    }


    void Update()
    {
        for (int index = 0; index < BubbleBools.Length; index++)
        {
            if (BubbleBools[index] == true && Activated)
            {
                if (animator != null)
                {
                    animator.enabled = false;
                }
                BubbleActions[index]();
                BubbleBools[index] = false;
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

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collided with: " + collision.gameObject.name);
    }
}
