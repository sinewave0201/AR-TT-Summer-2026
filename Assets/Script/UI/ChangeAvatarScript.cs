using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChangeAvatarScript : MonoBehaviour
{
    [Header("Avatars")]
    [SerializeField] private GameObject currentAvatar;
    [SerializeField] private GameObject currentAvatarPrefab;
    [SerializeField] private List<GameObject> avatarPrefabs = new List<GameObject>();
    [SerializeField] private SessionManager sessionManager;

    [Header("Avatar Spawn Transform")]
    [SerializeField] private Vector3 avatarSpawnPosition;
    private Quaternion avatarSpawnRotation = Quaternion.identity;

    [Header("Panels")]
    [SerializeField] private GameObject changeUI;
    [SerializeField] private GameObject generalUI;
    [SerializeField] private TMP_Text changeUITitle;
    [SerializeField] private Button quitChangeUIButton;

    [Header("Scroll View")]
    [Tooltip("Usually the Scroll View's Viewport/Content transform.")]
    [SerializeField] private Transform scrollViewContent;
    [Tooltip("A Button prefab containing a TMP_Text child.")]
    [SerializeField] private Button avatarOptionButtonPrefab;
    [SerializeField, Min(1f)] private float avatarOptionHeight = 50f;

    private readonly List<GameObject> generatedOptions = new List<GameObject>();
    private readonly Dictionary<int, TMP_Text> optionTexts = new Dictionary<int, TMP_Text>();
    private readonly Dictionary<int, Color> optionTextDefaultColors = new Dictionary<int, Color>();
    private static readonly Color SelectedTextColor = new Color32(0xD0, 0x2B, 0xFD, 0xFF);

    [Header("Notification")]
    [System.NonSerialized] public bool prefabPlaced;
    [System.NonSerialized] public bool sessionStart;
    [SerializeField] private TMP_Text notification;
    private Coroutine notificationCoroutine;

    private void Awake()
    {
        prefabPlaced = false;
        sessionStart = false;

        if (quitChangeUIButton != null)
        {
            quitChangeUIButton.onClick.AddListener(QuitChangeUI);
        }
    }

    private void OnDestroy()
    {
        if (quitChangeUIButton != null)
        {
            quitChangeUIButton.onClick.RemoveListener(QuitChangeUI);
        }
    }

    //use to mark start of session
    public void MarkStartSession(bool tf)
    {
        sessionStart = tf;
        Debug.Log($"MarkStartSession: prefabPlaced={prefabPlaced}, sessionStart={sessionStart}", this);
    }

    public void StartChange()
    {
        Debug.Log($"StartChange called: prefabPlaced={prefabPlaced}, sessionStart={sessionStart}", this);

        if (prefabPlaced && !sessionStart)
        {
            if (generalUI != null)
            {
                generalUI.SetActive(false);
            }

            if (changeUI != null)
            {
                changeUI.SetActive(true);
            }

            if (changeUITitle != null)
            {
                changeUITitle.text = "Change Avatar";
            }

            GenerateAvatarOptions();
        }

        else if (!prefabPlaced)
        {
            Debug.Log("Should call this");
            ShowNotification("Cannot change avatar since avatar is not placed");
        }

        else if (sessionStart)
        {
            ShowNotification("Cannot change avatar since you are in a session");
        }

    }
    //use to show notification

    private void ShowNotification(string msg)
    {
        if (notification == null)
        {
            Debug.LogWarning(msg, this);
            return;
        }

        if (!isActiveAndEnabled)
        {
            notification.text = msg;
            Debug.LogWarning(
                "ChangeAvatarScript is inactive, so it cannot clear the notification with a coroutine. " +
                "Put this script on the always-active AvatarChangeManager object and only disable the UI Canvas.",
                this);
            return;
        }

        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
        }

        notificationCoroutine = StartCoroutine(HideTextAfterSeconds(msg));
    }

    IEnumerator HideTextAfterSeconds(string msg)
    {
        notification.text = msg;
        yield return new WaitForSeconds(3f);
        notification.text = "";
        notificationCoroutine = null;
    }

    public void SetCurrentAvatar(
        GameObject avatar,
        GameObject avatarPrefab,
        Vector3 spawnPosition,
        Quaternion spawnRotation,
        SessionManager manager)
    {
        currentAvatar = avatar;
        currentAvatarPrefab = avatarPrefab;
        avatarSpawnPosition = spawnPosition;
        avatarSpawnRotation = spawnRotation;

        if (manager != null)
        {
            sessionManager = manager;
        }
    }

    private void GenerateAvatarOptions()
    {
        ClearGeneratedOptions();

        if (scrollViewContent == null || avatarOptionButtonPrefab == null)
        {
            Debug.LogError(
                "ChangeAvatarScript needs a Scroll View Content transform and an avatar option Button prefab.",
                this);
            return;
        }

        for (int i = 0; i < avatarPrefabs.Count; i++)
        {
            GameObject avatarPrefab = avatarPrefabs[i];
            if (avatarPrefab == null)
            {
                continue;
            }

            int selectedIndex = i;
            Button optionButton = Instantiate(avatarOptionButtonPrefab, scrollViewContent);
            optionButton.name = $"Avatar Option - {avatarPrefab.name}";

            RectTransform optionRect = optionButton.transform as RectTransform;
            if (optionRect != null)
            {
                optionRect.anchoredPosition = Vector2.zero;
                optionRect.localRotation = Quaternion.identity;
                optionRect.localScale = Vector3.one;
                optionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, avatarOptionHeight);
            }

            LayoutElement layoutElement = optionButton.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = optionButton.gameObject.AddComponent<LayoutElement>();
                layoutElement.minHeight = avatarOptionHeight;
                layoutElement.preferredHeight = avatarOptionHeight;
                layoutElement.flexibleHeight = 0f;
                layoutElement.flexibleWidth = 1f;
            }

            TMP_Text optionText = optionButton.GetComponentInChildren<TMP_Text>(true);
            if (optionText != null)
            {
                optionText.text = avatarPrefab.name;
                optionTexts[selectedIndex] = optionText;
                optionTextDefaultColors[selectedIndex] = optionText.color;
            }
            else
            {
                Debug.LogWarning(
                    $"The avatar option Button prefab has no TMP_Text child for '{avatarPrefab.name}'.",
                    optionButton);
            }

            optionButton.onClick.AddListener(() => SelectAvatar(selectedIndex));
            generatedOptions.Add(optionButton.gameObject);
        }

        UpdateSelectedOptionTextColor(0);

        if (scrollViewContent is RectTransform contentRect)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.MarkLayoutForRebuild(contentRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
    }

    public void SelectAvatar(int avatarIndex)
    {
        if (avatarIndex < 0 || avatarIndex >= avatarPrefabs.Count || avatarPrefabs[avatarIndex] == null)
        {
            Debug.LogWarning($"Cannot select avatar at invalid index {avatarIndex}.", this);
            return;
        }

        GameObject selectedAvatarPrefab = avatarPrefabs[avatarIndex];
        UpdateSelectedOptionTextColor(avatarIndex);

        if (selectedAvatarPrefab == currentAvatarPrefab)
        {
            return;
        }

        if (currentAvatar != null)
        {
            Destroy(currentAvatar);
        }

        currentAvatar = Instantiate(
            selectedAvatarPrefab,
            avatarSpawnPosition,
            avatarSpawnRotation);
        currentAvatarPrefab = selectedAvatarPrefab;

        if (sessionManager != null)
        {
            sessionManager.robotAnimator = currentAvatar.GetComponentInChildren<Animator>(true);
        }
    }

    public void QuitChangeUI()
    {
        if (changeUI != null)
        {
            changeUI.SetActive(false);
        }

        if (generalUI != null)
        {
            generalUI.SetActive(true);
        }
    }

    private void ClearGeneratedOptions()
    {
        foreach (GameObject option in generatedOptions)
        {
            if (option != null)
            {
                Destroy(option);
            }
        }

        generatedOptions.Clear();
        optionTexts.Clear();
        optionTextDefaultColors.Clear();
    }

    private void UpdateSelectedOptionTextColor(int selectedIndex)
    {
        foreach (KeyValuePair<int, TMP_Text> option in optionTexts)
        {
            if (option.Value == null)
            {
                continue;
            }

            option.Value.color = option.Key == selectedIndex
                ? SelectedTextColor
                : optionTextDefaultColors[option.Key];
        }
    }
}
