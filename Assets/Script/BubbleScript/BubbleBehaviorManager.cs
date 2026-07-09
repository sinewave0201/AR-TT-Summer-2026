using UnityEngine;
using System;
using TMPro;

public class BubbleBehaviorManager : MonoBehaviour
{
    public enum BubbleBehavior
    {
        None = -1,
        Fly = 0,
        Clean = 1,
        Bloom = 2,
        Burn = 3
    }

    //fly, clean, kick, burn
    public bool[] BubbleBools = {false, false, false, false};
    public bool Activated = false;
    public BubbleBehavior CurrentBehavior { get; private set; } =
        BubbleBehavior.None;
    private Action[] BubbleActions;
    private Rigidbody rb;
    private BubbleBloom bubbleBloom;
    private BubbleBurn bubbleBurn;
    private BubbleClean bubbleClean;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
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
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalScale = transform.localScale;

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
        bubbleBurn?.EndBurn();
        ResetBubbleTransform();
    }

    public void BubbleBehaviorEnd()
    {

        Activated = false;
        CurrentBehavior = BubbleBehavior.None;
        EndCleanBehavior();
        EndBloomBehavior();
        EndBurnBehavior();
        ResetBubbleTransform();
        EndFlyBehavior();

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

        gameObject.SetActive(false);
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

    private void ResetBubbleTransform()
    {
        transform.SetPositionAndRotation(originalPosition, originalRotation);
        transform.localScale = originalScale;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    void Update()
    {
        for (int index = 0; index < BubbleBools.Length; index++)
        {
            if (BubbleBools[index] == true && Activated)
            {
                BubbleActions[index]();
                BubbleBools[index] = false;
                Debug.Log($"Bubble Action performed, bubbleActivated = {Activated}", this);
            }
        }
    }

    void flyBubble()
    {
        CurrentBehavior = BubbleBehavior.Fly;
        Debug.Log("flyBubble Activated");

        //disable animator to enable flying
        if (animator != null)
        {
            animator.enabled = false;
        }

        bubbleBurn.DisableBurn();
        bubbleBurn.DisableKickInteraction();
        rb.isKinematic = false;
        rb.AddForce(Vector3.up * 5F, ForceMode.Force);
    }

    void cleanBubble()
    {
        //disable animator to enable cleaning
        if (animator != null)
        {
            animator.enabled = false;
        }

        CurrentBehavior = BubbleBehavior.Clean;
        bubbleBurn.DisableBurn();
        bubbleBurn.DisableKickInteraction();
        bubbleClean.StartClean();
    }

    void bloomBubble()
    {
        CurrentBehavior = BubbleBehavior.Bloom;
        Debug.Log("bloomBubble Activated");
        bubbleBurn.DisableBurn();
        bubbleBurn.DisableKickInteraction();
        bubbleBloom.StartBloom();
    }

    void burnBubble()
    {
        CurrentBehavior = BubbleBehavior.Burn;
        Debug.Log("burnBubble Activated");
        //disable animator to enable kicking
        if (animator != null)
        {
            animator.enabled = false;
        }

        bubbleBurn.EnableKickInteraction();
        bubbleBurn.EnableBurn();
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collided with: " + collision.gameObject.name);
    }
}
