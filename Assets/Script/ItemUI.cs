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
        ResizeToFitText();
    }

    private void ResizeToFitText()
    {
        contentTxt.ForceMeshUpdate();
        timeTxt.ForceMeshUpdate();

        RectTransform contentRect = contentTxt.rectTransform;
        Vector2 contentSize = contentRect.sizeDelta;
        contentSize.y = contentTxt.preferredHeight;
        contentRect.sizeDelta = contentSize;

        float preferredHeight = Mathf.Max(
            minHeight,
            verticalPadding + timeTxt.preferredHeight + spacing + contentTxt.preferredHeight);

        RectTransform rootRect = (RectTransform)transform;
        Vector2 rootSize = rootRect.sizeDelta;
        rootSize.y = preferredHeight;
        rootRect.sizeDelta = rootSize;

        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.preferredHeight = preferredHeight;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
    }
}
