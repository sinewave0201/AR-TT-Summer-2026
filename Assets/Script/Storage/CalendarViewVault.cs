using System;
using System.Globalization;
using UnityEngine;

public class CalendarViewVault : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private VaultManager vaultManager;
    [SerializeField] private WispCalendar calendar;

    [Header("UI Rendering")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject itemPrefab;

    private const string BubbleDateFormat = "yyyy-MM-dd HH:mm";

    private void Awake()
    {
        if (vaultManager == null)
        {
            vaultManager = FindFirstObjectByType<VaultManager>(FindObjectsInactive.Include);
        }

        if (calendar == null)
        {
            calendar = FindFirstObjectByType<WispCalendar>(FindObjectsInactive.Include);
        }
    }

    private void OnEnable()
    {
        if (calendar != null)
        {
            calendar.Initialize();
            calendar.OnDateChanged.AddListener(RenderSelectedDate);
        }

        RenderSelectedDate();
    }

    private void OnDisable()
    {
        if (calendar != null)
        {
            calendar.OnDateChanged.RemoveListener(RenderSelectedDate);
        }
    }

    public void RenderSelectedDate()
    {
        if (vaultManager == null)
        {
            Debug.LogError("CalendarViewVault needs a VaultManager reference.", this);
            return;
        }

        if (calendar == null)
        {
            Debug.LogError("CalendarViewVault needs a WispCalendar reference.", this);
            return;
        }

        if (content == null)
        {
            Debug.LogError("CalendarViewVault needs a content reference.", this);
            return;
        }

        if (itemPrefab == null)
        {
            Debug.LogError("CalendarViewVault needs an itemPrefab reference.", this);
            return;
        }

        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        DateTime selectedDate = calendar.SelectedDate.Date;

        foreach (VaultManager.BubbleVault bubble in vaultManager.vault)
        {
            if (!TryGetBubbleDate(bubble.bubbleCreatedDate, out DateTime bubbleDate))
            {
                Debug.LogWarning($"Could not parse bubble date: {bubble.bubbleCreatedDate}", this);
                continue;
            }

            if (bubbleDate.Date != selectedDate)
            {
                continue;
            }

            GameObject obj = Instantiate(itemPrefab, content);
            ItemUI itemUI = obj.GetComponent<ItemUI>();
            if (itemUI == null)
            {
                Debug.LogError("CalendarViewVault itemPrefab needs an ItemUI component.", obj);
                continue;
            }

            itemUI.SetData(bubble.bubbleCreatedDate, bubble.bubbleContent);
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
