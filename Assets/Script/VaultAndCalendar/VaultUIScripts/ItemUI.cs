using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class ItemUI : MonoBehaviour
{
    [Header("ItemUIs")]
    [SerializeField] private TMP_Text timeTxt;
    [SerializeField] private TMP_Text contentTxt;
    [SerializeField] private TMP_Text emotionTxt;

    [SerializeField] private float verticalPadding = 20f;
    [SerializeField] private float spacing = 4f;
    [SerializeField] private float minHeight = 116f;

    public void SetData(string time, string content, List<string> emotions)
    {
        timeTxt.text = time;
        contentTxt.text = content;
        
        emotionTxt.text = emotions.Count == 0? "None": string.Join(", ", emotions);
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
