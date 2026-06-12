using UnityEngine;
using TMPro;

public class ItemUI : MonoBehaviour
{
    public TMP_Text timeTxt;
    public TMP_Text contentTxt;

    public void SetData(string time, string content)
    {
        timeTxt.text = time;
        contentTxt.text = content;
    }
}
