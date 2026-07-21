using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class ItemUI : MonoBehaviour
{
    [Header("ItemUIs")]
    [SerializeField] private TMP_Text timeTxt;
    [SerializeField] private TMP_Text contentTxt;
    [SerializeField] private TMP_Text emotionHeaderTxt;
    [SerializeField] private TMP_Text emotionTxt;

    [SerializeField] private float verticalPadding = 20f;
    [SerializeField] private float spacing = 4f;
    [SerializeField] private float minHeight = 116f;

    public void SetData(string time, string content, List<string> emotions)
    {
        timeTxt.text = time;
        contentTxt.text = content;
        
        emotionTxt.text = emotions == null || emotions.Count == 0
            ? "None"
            : string.Join(", ", emotions);
        RefreshLayout();
    }

    public void RefreshLayout()
    {
        RectTransform timeRect = timeTxt.rectTransform;
        RectTransform contentRect = contentTxt.rectTransform;
        RectTransform emotionHeaderRect = emotionHeaderTxt.rectTransform;
        RectTransform emotionRect = emotionTxt.rectTransform;

        float timeWidth = Mathf.Max(1f, timeRect.rect.width);
        float timeHeight = timeTxt.GetPreferredValues(
            timeTxt.text,
            timeWidth,
            0f
        ).y;

        float contentWidth = Mathf.Max(1f, contentRect.rect.width);
        float contentHeight = contentTxt.GetPreferredValues(
            contentTxt.text,
            contentWidth,
            0f
        ).y;

        float emotionHeaderWidth = Mathf.Max(1f, emotionHeaderRect.rect.width);
        float emotionHeaderHeight = emotionHeaderTxt.GetPreferredValues(
            emotionHeaderTxt.text,
            emotionHeaderWidth,
            0f
        ).y;

        float emotionWidth = Mathf.Max(1f, emotionRect.rect.width);
        float emotionHeight = emotionTxt.GetPreferredValues(
            emotionTxt.text,
            emotionWidth,
            0f
        ).y;

        float emotionRowHeight = Mathf.Max(emotionHeaderHeight, emotionHeight);
        float topPadding = verticalPadding * 0.5f;
        float contentTop = topPadding + timeHeight + spacing;
        float emotionTop = contentTop + contentHeight + spacing;

        timeRect.anchoredPosition = new Vector2(timeRect.anchoredPosition.x, -topPadding);
        contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, -contentTop);
        emotionHeaderRect.anchoredPosition = new Vector2(emotionHeaderRect.anchoredPosition.x, -emotionTop);
        emotionRect.anchoredPosition = new Vector2(emotionRect.anchoredPosition.x, -emotionTop);

        timeRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, timeHeight);
        contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        emotionHeaderRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, emotionRowHeight);
        emotionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, emotionRowHeight);

        float preferredHeight = Mathf.Max(
            minHeight,
            emotionTop + emotionRowHeight + topPadding);

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
