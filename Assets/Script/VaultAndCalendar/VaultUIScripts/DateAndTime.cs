using UnityEngine;
using TMPro;
using System;

public class DateAndTime : MonoBehaviour
{
    public TMP_Text datenTime;

    // Update is called once per frame
    void Update()
    {
        datenTime.text = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
    }
}
