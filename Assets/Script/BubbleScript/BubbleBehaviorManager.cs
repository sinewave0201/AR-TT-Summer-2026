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
    private BubbleBloom bubbleBloom;
    private BubbleBurn bubbleBurn;
    private BubbleClean bubbleClean;
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
        bubbleBloom = GetComponent<BubbleBloom>();
        bubbleBurn = GetComponent<BubbleBurn>();
        bubbleClean = GetComponent<BubbleClean>();

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

        if (bubbleBloom == null)
        {
            bubbleBloom = gameObject.AddComponent<BubbleBloom>();
        }

        if (bubbleBurn == null)
        {
            bubbleBurn = gameObject.AddComponent<BubbleBurn>();
        }

        if (bubbleClean == null)
        {
            bubbleClean = gameObject.AddComponent<BubbleClean>();
        }

        BubbleActions = new Action[]
        {
            flyBubble,
            cleanBubble,
            bloomBubble,
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

    public void ResetBubblePosition()
    {
        if (bubbleBurn != null)
        {
            bubbleBurn.ResetBubblePosition();
        }
    }

    public void BubbleBehaviorEnd()
    {
        EndFlyBehavior();
        EndCleanBehavior();
        EndBloomBehavior();
        EndBurnBehavior();

        Array.Clear(BubbleBools, 0, BubbleBools.Length);

        if (animator != null)
        {
            animator.enabled = true;
        }
    }

    private void EndFlyBehavior()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    private void EndCleanBehavior()
    {
        bubbleClean?.ResetBubbleClean();
    }

    private void EndBloomBehavior()
    {
        bubbleBloom?.EndBloom();
    }

    private void EndBurnBehavior()
    {
        bubbleBurn?.EndBurn();
    }

    void Update()
    {
        for (int index = 0; index < BubbleBools.Length; index++)
        {
            if (BubbleBools[index] == true && Activated)
            {
                BubbleActions[index]();
                BubbleBools[index] = false;
                Debug.Log("Bubble Action performed");
            }
        }
    }

    void flyBubble()
    {
        Debug.Log("flyBubble Activated");
        bubbleBurn.DisableBurn();
        bubbleBurn.DisableKickInteraction();
        rb.isKinematic = false;
        rb.AddForce(Vector3.up * 5F, ForceMode.Force);
    }

    void cleanBubble()
    {
        bubbleBurn.DisableBurn();
        bubbleBurn.DisableKickInteraction();
        bubbleClean.StartClean();
    }

    void bloomBubble()
    {
        Debug.Log("bloomBubble Activated");
        bubbleBurn.DisableBurn();
        bubbleBurn.DisableKickInteraction();
        bubbleBloom.StartBloom();
    }

    void burnBubble()
    {
        Debug.Log("burnBubble Activated");
        bubbleBurn.EnableKickInteraction();
        bubbleBurn.EnableBurn();
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collided with: " + collision.gameObject.name);
    }
}
