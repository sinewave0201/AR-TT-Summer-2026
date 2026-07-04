using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BubbleButtonReference :
    MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    [SerializeField] private TMP_Text buttonText;

    private Button button;
    private CanvasGroup canvasGroup;
    private BubbleBehaviorManager bubbleBehaviorManager;
    private BubbleBloom bubbleBloom;
    private BubbleBehaviorManager.BubbleBehavior displayedBehavior =
        BubbleBehaviorManager.BubbleBehavior.None;

    private void Awake()
    {
        button = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }

        ResolveReferences();
        SetVisible(false);
        RefreshForBehavior();
    }

    private void Update()
    {
        if (bubbleBehaviorManager == null)
        {
            ResolveReferences();
        }

        RefreshForBehavior();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        if (bubbleBehaviorManager != null &&
            bubbleBehaviorManager.CurrentBehavior ==
            BubbleBehaviorManager.BubbleBehavior.Burn)
        {
            bubbleBehaviorManager.ResetBubblePosition();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (bubbleBehaviorManager != null &&
            bubbleBehaviorManager.CurrentBehavior ==
            BubbleBehaviorManager.BubbleBehavior.Bloom)
        {
            ResolveBloom();
            bubbleBloom?.SetWatering(true);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        bubbleBloom?.SetWatering(false);
    }

    private void OnDisable()
    {
        bubbleBloom?.SetWatering(false);
    }

    public void SetText(string text)
    {
        if (buttonText == null)
        {
            buttonText = GetComponentInChildren<TMP_Text>(true);
        }

        if (buttonText != null)
        {
            buttonText.text = text;
        }
    }

    public void SetVisible(bool visible)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private void ResolveReferences()
    {
        bubbleBehaviorManager =
            FindFirstObjectByType<BubbleBehaviorManager>(FindObjectsInactive.Include);
        ResolveBloom();
    }

    private void ResolveBloom()
    {
        if (bubbleBloom == null)
        {
            bubbleBloom =
                FindFirstObjectByType<BubbleBloom>(FindObjectsInactive.Include);
        }
    }

    private void RefreshForBehavior()
    {
        BubbleBehaviorManager.BubbleBehavior behavior =
            bubbleBehaviorManager != null
                ? bubbleBehaviorManager.CurrentBehavior
                : BubbleBehaviorManager.BubbleBehavior.None;

        if (behavior == displayedBehavior)
        {
            return;
        }

        displayedBehavior = behavior;
        bool isBloom =
            behavior == BubbleBehaviorManager.BubbleBehavior.Bloom;
        bool isBurn =
            behavior == BubbleBehaviorManager.BubbleBehavior.Burn;

        if (isBloom)
        {
            SetText("Water the flower");
        }
        else if (isBurn)
        {
            SetText("Reset position");
        }

        SetVisible(isBloom || isBurn);
    }
}
