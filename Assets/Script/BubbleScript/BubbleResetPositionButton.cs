using UnityEngine;
using UnityEngine.UI;

public class BubbleResetPositionButton : MonoBehaviour
{
    private Button button;
    private bool visible;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(ResetBubblePosition);
        }

        gameObject.SetActive(visible);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(ResetBubblePosition);
        }
    }

    public void SetVisible(bool visible)
    {
        this.visible = visible;
        gameObject.SetActive(visible);
    }

    public void ResetBubblePosition()
    {
        BubbleBehaviorManager bubbleBehaviorManager =
            FindFirstObjectByType<BubbleBehaviorManager>(FindObjectsInactive.Include);

        if (bubbleBehaviorManager == null)
        {
            Debug.LogWarning("Reset bubble position button could not find a BubbleBehaviorManager.", this);
            return;
        }

        bubbleBehaviorManager.ResetBubblePosition();
    }
}
