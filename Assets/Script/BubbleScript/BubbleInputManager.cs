using UnityEngine;
using TMPro;

public class BubbleInputManager : MonoBehaviour
{
    public TMP_InputField inputField;
    public TMP_Text bubbleText;

    // Update is called once per frame
    public void finishInput()
    {
        bubbleText.text = inputField.text;
    }
}
