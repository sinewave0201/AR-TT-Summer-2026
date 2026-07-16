using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChangeAvatarScriptARMarker : MonoBehaviour
{
    [Header("Avatars")]
    [SerializeField] private GameObject currentAvatar;
    [SerializeField] private GameObject currentAvatarPrefab;
    [SerializeField] private List<GameObject> avatarPrefabs = new List<GameObject>();
    [SerializeField] private List<Vector3> avatarPosition = new List<Vector3>();
    [SerializeField] private List<Quaternion> avatarRotation = new List<Quaternion>();
    [SerializeField] private SessionManager sessionManager;

    [Header("Prefab Initiation Source")]
    [SerializeField] private TapToPlaceManager tapToPlaceManager;
    [SerializeField] private MultipleImageTracker multipleImageTracker;

    [Header("Marker-relative Avatar Spawn Transform")]
    [SerializeField] private Transform avatarSpawnParent;
    [SerializeField] private Vector3 avatarSpawnLocalPosition;
    [SerializeField] private Quaternion avatarSpawnLocalRotation = Quaternion.identity;
    [SerializeField] private Vector3 avatarSpawnScale = Vector3.one;

    [Header("Panels")]
    [SerializeField] private GameObject changeUI;
    [SerializeField] private GameObject generalUI;
    [SerializeField] private TMP_Text changeUITitle;
    [SerializeField] private Button quitChangeUIButton;

    [Header("Scroll View")]
    [SerializeField] private Transform scrollViewContent;
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
        quitChangeUIButton?.onClick.AddListener(QuitChangeUI);
    }

    private void OnDestroy()
    {
        quitChangeUIButton?.onClick.RemoveListener(QuitChangeUI);
    }

    public void MarkStartSession(bool value)
    {
        sessionStart = value;
    }

    public void SetMarkerTracking(bool isTracking)
    {
        prefabPlaced = isTracking && currentAvatar != null;

        if (!isTracking && changeUI != null && changeUI.activeSelf)
        {
            QuitChangeUI();
        }
    }

    public void StartChange()
    {
        bool prefabInitiated = IsPrefabInitiated();

        if (prefabInitiated && !sessionStart)
        {
            generalUI?.SetActive(false);
            changeUI?.SetActive(true);

            if (changeUITitle != null)
            {
                changeUITitle.text = "Change Avatar";
            }

            GenerateAvatarOptions();
        }
        else if (!prefabInitiated)
        {
            ShowNotification("Cannot change avatar until the AR marker is tracked");
        }
        else
        {
            ShowNotification("Cannot change avatar since you are in a session");
        }
    }

    private bool IsPrefabInitiated()
    {
        if (tapToPlaceManager != null)
        {
            return tapToPlaceManager.IsPrefabInitiated;
        }

        if (multipleImageTracker != null)
        {
            return multipleImageTracker.IsPrefabInitiated;
        }

        return prefabPlaced;
    }

    public void SetCurrentAvatar(
        GameObject avatar,
        GameObject avatarPrefab,
        Transform spawnParent,
        Vector3 spawnLocalPosition,
        Quaternion spawnLocalRotation,
        Vector3 spawnScale,
        SessionManager manager)
    {
        currentAvatar = avatar;
        currentAvatarPrefab = avatarPrefab;
        avatarSpawnParent = spawnParent;
        avatarSpawnLocalPosition = spawnLocalPosition;
        avatarSpawnLocalRotation = spawnLocalRotation;
        avatarSpawnScale = spawnScale;

        if (manager != null)
        {
            sessionManager = manager;
        }
    }

    public GameObject InitializeForMarker(
        Transform spawnParent,
        Vector3 spawnLocalPosition,
        Quaternion spawnLocalRotation,
        Vector3 spawnScale,
        SessionManager manager)
    {
        if (spawnParent == null)
        {
            Debug.LogError(
                "Cannot initialize the marker avatar without a spawn parent.",
                this);
            return null;
        }

        if (avatarPrefabs.Count == 0 || avatarPrefabs[0] == null)
        {
            Debug.LogError(
                "ChangeAvatarScriptARMarker needs a default avatar in the first avatar-prefab slot.",
                this);
            return null;
        }

        GameObject defaultAvatarPrefab = avatarPrefabs[0];

        // Do not stack another avatar on a marker that already owns one. This can
        // happen if more than one tracker instance receives the same image event.
        Transform existingAvatar = spawnParent.Find(defaultAvatarPrefab.name);
        if (existingAvatar != null)
        {
            GameObject avatarObject = existingAvatar.gameObject;
            avatarObject.transform.SetLocalPositionAndRotation(
                spawnLocalPosition,
                spawnLocalRotation);
            avatarObject.transform.localScale = spawnScale;

            SetCurrentAvatar(
                avatarObject,
                defaultAvatarPrefab,
                spawnParent,
                spawnLocalPosition,
                spawnLocalRotation,
                spawnScale,
                manager);

            if (sessionManager != null)
            {
                sessionManager.robotAnimator =
                    avatarObject.GetComponentInChildren<Animator>(true);
            }

            Debug.LogWarning(
                $"Marker already contains avatar '{defaultAvatarPrefab.name}'. Reusing it.",
                avatarObject);
            return avatarObject;
        }

        GameObject avatar = Instantiate(defaultAvatarPrefab, spawnParent);
        avatar.name = defaultAvatarPrefab.name;
        avatar.transform.SetLocalPositionAndRotation(
            spawnLocalPosition,
            spawnLocalRotation);
        avatar.transform.localScale = spawnScale;
        avatar.SetActive(true);

        SetCurrentAvatar(
            avatar,
            defaultAvatarPrefab,
            spawnParent,
            spawnLocalPosition,
            spawnLocalRotation,
            spawnScale,
            manager);

        prefabPlaced = true;

        if (sessionManager != null)
        {
            sessionManager.robotAnimator = avatar.GetComponentInChildren<Animator>(true);
        }

        Debug.Log(
            $"Spawned marker avatar '{avatar.name}' at local position {spawnLocalPosition} " +
            $"with scale {spawnScale}.",
            avatar);

        return avatar;
    }

    public void SelectAvatar(int avatarIndex)
    {
        if (avatarIndex < 0 || avatarIndex >= avatarPrefabs.Count ||
            avatarPrefabs[avatarIndex] == null || avatarSpawnParent == null)
        {
            Debug.LogWarning($"Cannot select avatar at index {avatarIndex}.", this);
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

        Vector3 positionOffset = avatarIndex < avatarPosition.Count
            ? avatarPosition[avatarIndex]
            : Vector3.zero;
        Quaternion rotationOffset = avatarIndex < avatarRotation.Count
            ? avatarRotation[avatarIndex]
            : Quaternion.identity;

        currentAvatar = Instantiate(selectedAvatarPrefab, avatarSpawnParent);
        currentAvatar.transform.SetLocalPositionAndRotation(
            avatarSpawnLocalPosition + positionOffset,
            avatarSpawnLocalRotation * rotationOffset);
        currentAvatar.transform.localScale = avatarSpawnScale;
        currentAvatar.SetActive(true);
        currentAvatarPrefab = selectedAvatarPrefab;

        if (sessionManager != null)
        {
            sessionManager.robotAnimator = currentAvatar.GetComponentInChildren<Animator>(true);
        }
    }

    public void QuitChangeUI()
    {
        changeUI?.SetActive(false);
        generalUI?.SetActive(true);
    }

    private void GenerateAvatarOptions()
    {
        ClearGeneratedOptions();

        if (scrollViewContent == null || avatarOptionButtonPrefab == null)
        {
            Debug.LogError("AR marker avatar UI references are not assigned.", this);
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

            if (optionButton.transform is RectTransform optionRect)
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
            }

            layoutElement.minHeight = avatarOptionHeight;
            layoutElement.preferredHeight = avatarOptionHeight;
            layoutElement.flexibleHeight = 0f;
            layoutElement.flexibleWidth = 1f;

            TMP_Text optionText = optionButton.GetComponentInChildren<TMP_Text>(true);
            if (optionText != null)
            {
                optionText.text = avatarPrefab.name;
                optionTexts[selectedIndex] = optionText;
                optionTextDefaultColors[selectedIndex] = optionText.color;
            }

            optionButton.onClick.AddListener(() => SelectAvatar(selectedIndex));
            generatedOptions.Add(optionButton.gameObject);
        }

        int currentIndex = avatarPrefabs.IndexOf(currentAvatarPrefab);
        UpdateSelectedOptionTextColor(currentIndex);

        if (scrollViewContent is RectTransform contentRect)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
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
        foreach (var option in optionTexts)
        {
            if (option.Value != null)
            {
                option.Value.color = option.Key == selectedIndex
                    ? SelectedTextColor
                    : optionTextDefaultColors[option.Key];
            }
        }
    }

    private void ShowNotification(string message)
    {
        if (notification == null)
        {
            Debug.LogWarning(message, this);
            return;
        }

        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
        }

        notificationCoroutine = StartCoroutine(HideTextAfterSeconds(message));
    }

    private IEnumerator HideTextAfterSeconds(string message)
    {
        notification.text = message;
        yield return new WaitForSeconds(3f);
        notification.text = string.Empty;
        notificationCoroutine = null;
    }
}
