using UnityEngine;
using TMPro;

public class BubbleInputManager : MonoBehaviour
{
    public TMP_InputField inputField;
    public TMP_Text bubbleText;

    private void Awake()
{
    if (inputField == null)
    {
        inputField = FindFirstObjectByType<TMP_InputField>();
    }
}
    public void finishInput()
    {
        if (inputField != null)
        {
            bubbleText.text = inputField.text;
        }
    }
}
