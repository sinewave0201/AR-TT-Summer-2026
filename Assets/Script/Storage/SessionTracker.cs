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

    private const string CompletedText = "You have completed a session today!";
    private const string NotCompletedText = "You have not complete a session today! Click the robot to start session.";

    public void SetStatus()
    {
        if (status == null)
        {
            Debug.LogError("SessionTracker needs a status text reference.", this);
            return;
        }

        if (vaultManager == null)
        {
            Debug.LogError("SessionTracker needs a VaultManager reference.", this);
            status.text = NotCompletedText;
            return;
        }

        DateTime today = DateTime.Now.Date;
        bool hasCompletedSessionToday = false;

        foreach (VaultManager.BubbleVault bubble in vaultManager.vault)
        {
            if (!TryGetBubbleDate(bubble.bubbleCreatedDate, out DateTime bubbleDate))
            {
                Debug.Log("bubble not legal");
                continue;
            }

            if (bubbleDate.Date == today)
            {
                hasCompletedSessionToday = true;
                break;
            }
        }

        status.text = hasCompletedSessionToday ? CompletedText : NotCompletedText;
    }

    private void OnEnable()
    {
        if (vaultManager != null)
        {
            vaultManager.VaultChanged += SetStatus;
        }

        SetStatus();
    }

    private void OnDisable()
    {
        if (vaultManager != null)
        {
            vaultManager.VaultChanged -= SetStatus;
        }
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
