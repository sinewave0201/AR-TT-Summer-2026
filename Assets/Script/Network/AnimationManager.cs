using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    public enum RobotAnimation : byte
    {
        Idle,
        Wave,
        Nod,
        Agreeing,
        GetBubble
    }

    [SerializeField] private Animator robotAnimator;

    private void Awake()
    {
        if (robotAnimator == null)
        {
            robotAnimator = GetComponentInChildren<Animator>(true);
        }
    }

    public void Play(RobotAnimation animation)
    {
        if (robotAnimator == null)
        {
            Debug.LogWarning("AnimationManager cannot find the Robot Animator.", this);
            return;
        }

        if (animation == RobotAnimation.Idle)
        {
            robotAnimator.Play("Idle", 0, 0f);
            return;
        }

        robotAnimator.SetTrigger(animation.ToString());
    }
}
