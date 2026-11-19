using UnityEngine;

public class BubbleBloom : MonoBehaviour
{
    [SerializeField] private Animator bubbleAnimator;
    [SerializeField] private string bubbleBloomState = "Bloom";

    private void Awake()
    {
        if (bubbleAnimator == null)
        {
            bubbleAnimator = GetComponent<Animator>();
        }
    }

    public void StartBloom()
    {
        if (bubbleAnimator == null)
        {
            Debug.LogWarning(
                "BubbleBloom needs an Animator to play the Bloom animation.",
                this);
            return;
        }

        bubbleAnimator.enabled = true;
        bubbleAnimator.Play(bubbleBloomState, 0, 0f);
        bubbleAnimator.Update(0f);
    }

    public void EndBloom()
    {
        // The bubble Animator owns the complete visual sequence.
    }
}
