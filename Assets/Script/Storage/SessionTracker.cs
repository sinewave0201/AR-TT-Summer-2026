using UnityEngine;
using TMPro;
using System;
using System.Globalization;

public class SessionTracker : MonoBehaviour
{
    public TMP_Text status;
    [Header("Data")]
    [SerializeField] private VaultManager vaultManager;
    private const string BubbleDateFormat = "yyyy-MM-dd HH:mm";
    public void SetStatus()
    {
        DateTime nowDate = DateTime.Now;

        foreach (VaultManager.BubbleVault bubble in vaultManager.vault)
        {
            if (!TryGetBubbleDate(bubble.bubbleCreatedDate, out DateTime bubbleDate))
            {
                Debug.Log("bubble not legal");
                continue;
            }

            if (bubbleDate.Date != nowDate)
            {
                status.text = "You have not complete a session today! Click the robot to start session.";
                continue;
            }

            status.text = "You have completed a session today!";
        }
    }

    void Start()
    {
        SetStatus();
    }



    private bool TryGetBubbleDate(string dateText, out DateTime date)
    {
        return DateTime.TryParseExact(
            dateText,
            BubbleDateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date)
            || DateTime.TryParse(dateText, out date);
    }
}
