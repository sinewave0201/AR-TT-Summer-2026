using UnityEngine;
using System;
using TMPro;

public class BubbleBehaviorManager : MonoBehaviour
{
    private static readonly int EndParameter = Animator.StringToHash("End");

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
    public BubbleBehavior CurrentBehavior { get; private set; } =
        BubbleBehavior.None;

    [Header("bubble activation")]
    public bool Activated = false;

    //private stuff
    private Action[] BubbleActions;
    private Rigidbody rb;
    private BubbleBloom bubbleBloom;
    private BubbleBurn bubbleBurn;
    private BubbleClean bubbleClean;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;


    [Header("Bubble animator and Bubble Text")]
    public Animator animator;
    public TMP_Text bubbleText;

    [Header("Colliders")]
    [SerializeField]private MeshCollider meshCollider;
    [SerializeField]private SphereCollider sphereCollider;

    [Header("Update Animator Logic")]
    [SerializeField] private int layerIndex = 0;

    private AnimationClip previousClip;
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

        // The Animator owns MeshRenderer visibility. End returns every
        // behavior animation to the invisible Default state.
        if (animator != null)
        {
            animator.enabled = true;
            animator.SetBool(EndParameter, true);
        }

        //set the collider back
        meshCollider.enabled = true;
        sphereCollider.enabled = false;

        Debug.Log("Bubble behavior ended; Animator End set to true.", this);
    }

    public void BubbleBehaviorActivate()
    {
        if (animator != null)
        {
            animator.enabled = true;
            animator.SetBool(EndParameter, false);
        }

        Activated = true;
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
                //set the collider to be sphere collider
                meshCollider.enabled = false;
                sphereCollider.enabled = true;

                BubbleActions[index]();
                BubbleBools[index] = false;
                Debug.Log($"Bubble Action performed, bubbleActivated = {Activated}", this);
            }
        }

        UpdateAnimationClipChange();
    }

    void flyBubble()
    {
        CurrentBehavior = BubbleBehavior.Fly;
        Debug.Log("flyBubble Activated");

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
        // if (animator != null)
        // {
        //     animator.enabled = true;
        // }

        CurrentBehavior = BubbleBehavior.Bloom;
        Debug.Log("bloomBubble Activated");
        bubbleBurn.DisableBurn();
        bubbleBurn.DisableKickInteraction();
        bubbleBloom.StartBloom();
    }

    void burnBubble()
    {
        if (animator != null)
        {
            animator.enabled = false;
        }
        CurrentBehavior = BubbleBehavior.Burn;
        Debug.Log("burnBubble Activated");



        bubbleBurn.EnableKickInteraction();
        bubbleBurn.EnableBurn();
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collided with: " + collision.gameObject.name);
    }

    #region Update Animator Logic
    private void UpdateAnimationClipChange()
    {
        if (animator == null || !animator.enabled)
        {
            return;
        }
        
        AnimationClip currentClip = GetActiveClip();
        if (currentClip == null || currentClip == previousClip)
        {
            return;
        }

        Debug.Log(
            $"Bubble Animator changed clip: " +
            $"{previousClip?.name ?? "None"} -> {currentClip.name}",
            animator
        );

        previousClip = currentClip;
    }

    private AnimationClip GetActiveClip()
    {
        // During a transition, report the incoming clip immediately.
        if (animator.IsInTransition(layerIndex))
        {
            AnimatorClipInfo[] nextClips =
                animator.GetNextAnimatorClipInfo(layerIndex);

            if (nextClips.Length > 0)
            {
                return GetHighestWeightClip(nextClips);
            }
        }

        AnimatorClipInfo[] currentClips =
            animator.GetCurrentAnimatorClipInfo(layerIndex);

        return currentClips.Length > 0
            ? GetHighestWeightClip(currentClips)
            : null;
    }

    private static AnimationClip GetHighestWeightClip(
        AnimatorClipInfo[] clips)
    {
        AnimatorClipInfo result = clips[0];

        for (int index = 1; index < clips.Length; index++)
        {
            if (clips[index].weight > result.weight)
            {
                result = clips[index];
            }
        }

        return result.clip;
    }
    #endregion
}
