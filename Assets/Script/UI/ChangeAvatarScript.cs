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
    [SerializeField] private Vector3 avatarSpawnEulerAngles;

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

    [Header("Notification")]
    public bool prefabPlaced = true;
    public bool sessionStart = false;
    [SerializeField] private TMP_Text notification;

    private void Awake()
    {
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
    }

    public void StartChange()
    {
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
            StartCoroutine(HideTextAfterSeconds("Cannot change avatar since avatar is not placed"));
        }

        else if (sessionStart)
        {
            StartCoroutine(HideTextAfterSeconds("Cannot change avatar since you are in a session"));
        }

    }
    //use to show notification
    

    IEnumerator HideTextAfterSeconds(string msg)
    {
        notification.text = msg;
        yield return new WaitForSeconds(3f);
        notification.text = "";
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
        avatarSpawnEulerAngles = spawnRotation.eulerAngles;

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
            Quaternion.Euler(avatarSpawnEulerAngles));
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
    }
}
