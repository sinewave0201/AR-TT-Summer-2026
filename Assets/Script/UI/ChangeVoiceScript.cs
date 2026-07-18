using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChangeVoiceScript : MonoBehaviour
{
    [System.Serializable]
    public struct audioCollec
    {
        public string collecName;
        public List<AudioClip> clipList;
    }
    
    [Header("Voice")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private List<audioCollec> audioClipCollec = new List<audioCollec>();

    [Header("Panels")]
    [SerializeField] private List<GameObject> changeUI;
    [SerializeField] private GameObject generalUI;
    [SerializeField] private TMP_Text changeUITitle;
    [SerializeField] private Button quitChangeUIButton;

    [Header("Scroll View")]
    [Tooltip("Usually the Scroll View's Viewport/Content transform.")]
    [SerializeField] private Transform scrollViewContent;
    [Tooltip("A Button prefab containing a TMP_Text child.")]
    [SerializeField] private Button voiceOptionButtonPrefab;
    [SerializeField, Min(1f)] private float voiceOptionHeight = 50f;


    //logic to store the current list and clip
    [Header("Current Collection and clip")]
    private audioCollec currAudCollec;
    public List<AudioClip> currListofClip;

    //logic for changing the color for clicked button
    private readonly List<GameObject> generatedOptions = new List<GameObject>();
    private readonly Dictionary<int, TMP_Text> optionTexts = new Dictionary<int, TMP_Text>();
    private readonly Dictionary<int, Color> optionTextDefaultColors = new Dictionary<int, Color>();
    private static readonly Color SelectedTextColor = new Color32(0xD0, 0x2B, 0xFD, 0xFF);

    private void Awake()
    {
        if (quitChangeUIButton != null)
        {
            quitChangeUIButton.onClick.AddListener(QuitChangeUI);
        }
        currAudCollec = audioClipCollec[0];
        currListofClip = currAudCollec.clipList;
    }

    private void OnDestroy()
    {
        if (quitChangeUIButton != null)
        {
            quitChangeUIButton.onClick.RemoveListener(QuitChangeUI);
        }
    }

    public void StartChange()
    {
        if (generalUI != null)
        {
            generalUI.SetActive(false);
        }

        if (changeUI != null)
        {
            for (int i = 0; i < changeUI.Count; i ++){
                changeUI[i].SetActive(true);
            }
        }

        if (changeUITitle != null)
        {
            changeUITitle.text = "Change Voice";
        }

        GenerateVoiceOptions();
    }

    private void GenerateVoiceOptions()
    {
        ClearGeneratedOptions();

        if (scrollViewContent == null || voiceOptionButtonPrefab == null)
        {
            Debug.LogError(
                "ChangeVoiceScript needs a Scroll View Content transform and a voice option Button prefab.",
                this);
            return;
        }

        for (int i = 0; i < audioClipCollec.Count; i++)
        {
            audioCollec audCollec = audioClipCollec[i];
            List<AudioClip> listofClip = audCollec.clipList;
            AudioClip clip = listofClip[0];//default preview using first clip
            if (clip == null)
            {
                continue;
            }

            int selectedIndex = i;
            Button optionButton = Instantiate(voiceOptionButtonPrefab, scrollViewContent);
            optionButton.name = $"Voice Option - {audCollec.collecName}";

            RectTransform optionRect = optionButton.transform as RectTransform;
            if (optionRect != null)
            {
                optionRect.anchoredPosition = Vector2.zero;
                optionRect.localRotation = Quaternion.identity;
                optionRect.localScale = Vector3.one;
                optionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, voiceOptionHeight);
            }

            //if the prefab does not have a layout element
            //add that and modify the data
            LayoutElement layoutElement = optionButton.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = optionButton.gameObject.AddComponent<LayoutElement>();
                layoutElement.minHeight = voiceOptionHeight;
                layoutElement.preferredHeight = voiceOptionHeight;
                layoutElement.flexibleHeight = 0f;
                layoutElement.flexibleWidth = 1f;
            }



            TMP_Text optionText = optionButton.GetComponentInChildren<TMP_Text>(true);
            if (optionText != null)
            {
                optionText.text = audCollec.collecName;
                optionTexts[selectedIndex] = optionText;
                optionTextDefaultColors[selectedIndex] = optionText.color;
            }
            else
            {
                Debug.LogWarning(
                    $"The voice option Button prefab has no TMP_Text child for '{clip.name}'.",
                    optionButton);
            }

            optionButton.onClick.AddListener(() => SelectVoice(selectedIndex));
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

    public void SelectVoice(int optionIndex)
    {
        //logic to avoid null ref
        if (audioSource == null)
        {
            Debug.LogError("ChangeVoiceScript needs an AudioSource reference.", this);
            return;
        }

        if (!audioSource.gameObject.activeInHierarchy || !audioSource.enabled)
        {
            Debug.LogError(
                "The selected AudioSource is disabled or belongs to an inactive GameObject.",
                audioSource);
            return;
        }

        if (optionIndex < 0 || optionIndex >= audioClipCollec.Count)
        {
            Debug.LogWarning($"Cannot select voice at invalid clip index {optionIndex}.", this);
            return;
        }

        UpdateSelectedOptionTextColor(optionIndex);

        //update the current audiocollec and list of clips
        currAudCollec = audioClipCollec[optionIndex];
        currListofClip = currAudCollec.clipList;
        Debug.Log($"curList of clip change to {currAudCollec.collecName}");


        //logic to play the previews
        audioSource.Stop();
        audioSource.clip = currListofClip[0];
        audioSource.Play();

        if (!audioSource.isPlaying)
        {
            Debug.LogWarning(
                $"AudioSource did not start playing '{audioSource.clip.name}'. Check the clip's load state and AudioListener.",
                audioSource);
        }

        //logic to update the set of clips
    }

    public void QuitChangeUI()
    {
        if (changeUI != null)
        {
            for (int i = 0; i < changeUI.Count; i ++){
                changeUI[i].SetActive(false);
            }
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
