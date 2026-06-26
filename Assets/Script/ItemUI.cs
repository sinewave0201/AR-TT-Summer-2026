using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    public TMP_Text timeTxt;
    public TMP_Text contentTxt;

    [SerializeField] private float verticalPadding = 20f;
    [SerializeField] private float spacing = 4f;
    [SerializeField] private float minHeight = 116f;

    public void SetData(string time, string content)
    {
        timeTxt.text = time;
        contentTxt.text = content;
        RefreshLayout();
    }

    public void RefreshLayout()
    {
        RectTransform contentRect = contentTxt.rectTransform;
        float contentWidth = Mathf.Max(1f, contentRect.rect.width);
        float contentHeight = contentTxt.GetPreferredValues(
            contentTxt.text,
            contentWidth,
            0f
        ).y;

        contentRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            contentHeight
        );

        float timeWidth = Mathf.Max(1f, timeTxt.rectTransform.rect.width);
        float timeHeight = timeTxt.GetPreferredValues(
            timeTxt.text,
            timeWidth,
            0f
        ).y;

        float preferredHeight = Mathf.Max(
            minHeight,
            verticalPadding + timeHeight + spacing + contentHeight);

        RectTransform rootRect = (RectTransform)transform;
        rootRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            preferredHeight
        );

        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.preferredHeight = preferredHeight;
        }

        LayoutRebuilder.MarkLayoutForRebuild(rootRect);
    }
}
