using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChangeBaseModelScript : MonoBehaviour
{
    public GameObject CurrentModelPrefab => currentModelPrefab;

    [Header("Models")]
    public GameObject CurrentModel;
    [SerializeField] private GameObject currentModelPrefab;
    [SerializeField] private List<GameObject> BaseModelPrefabs = new List<GameObject>();

    [Header("Model Spawn Transform")]
    [SerializeField] private Vector3 ModelSpawnPosition;
    [SerializeField] private Vector3 ModelSpawnRotation;

    [Header("Model Preview")]
    [SerializeField] private ModelPreviewScript modelPreview;
    [SerializeField] private DIYManager diyManager;

    [Header("Panels")]
    [SerializeField] private GameObject changeUI;
    [SerializeField] private GameObject generalUI;
    [SerializeField] private Button quitChangeUIButton;

    [Header("Scroll View")]
    [Tooltip("Usually the Scroll View's Viewport/Content transform.")]
    [SerializeField] private Transform scrollViewContent;
    [Tooltip("A Button prefab containing a TMP_Text child.")]
    [SerializeField] private Button ModelOptionButtonPrefab;
    [SerializeField, Min(1f)] private float ModelOptionHeight = 50f;

    [Header("Selected Option")]
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
    }

    private void OnDestroy()
    {
        if (quitChangeUIButton != null)
        {
            quitChangeUIButton.onClick.RemoveListener(QuitChangeUI);
        }
    }

    public void SetCurrentModel(
        GameObject model,
        GameObject modelPrefab,
        Vector3 spawnPosition,
        Vector3 spawnRotation)
    {
        CurrentModel = model;
        currentModelPrefab = modelPrefab;
        ModelSpawnPosition = spawnPosition;
        ModelSpawnRotation = spawnRotation;
        modelPreview?.SetPrefab(CurrentModel);
        diyManager?.RegisterModel(CurrentModel);
    }

    public void StartChange()
    {
        if (generalUI != null)
        {
            generalUI.SetActive(false);
        }

        if (changeUI != null)
        {
            changeUI.SetActive(true);
        }

        GenerateModelOptions();
    }

    private void GenerateModelOptions()
    {
        ClearGeneratedOptions();

        if (scrollViewContent == null || ModelOptionButtonPrefab == null)
        {
            Debug.LogError(
                "ChangeBaseModelScript needs a Scroll View Content transform and a model option Button prefab.",
                this);
            return;
        }

        int selectedOptionIndex = -1;

        for (int i = 0; i < BaseModelPrefabs.Count; i++)
        {
            GameObject modelPrefab = BaseModelPrefabs[i];
            if (modelPrefab == null)
            {
                continue;
            }

            int selectedIndex = i;
            Button optionButton = Instantiate(ModelOptionButtonPrefab, scrollViewContent);
            optionButton.name = $"Base Model Option - {modelPrefab.name}";

            if (optionButton.transform is RectTransform optionRect)
            {
                optionRect.anchoredPosition = Vector2.zero;
                optionRect.localRotation = Quaternion.identity;
                optionRect.localScale = Vector3.one;
                optionRect.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    ModelOptionHeight);
            }

            LayoutElement layoutElement = optionButton.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = optionButton.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = ModelOptionHeight;
            layoutElement.preferredHeight = ModelOptionHeight;
            layoutElement.flexibleHeight = 0f;
            layoutElement.flexibleWidth = 1f;

            TMP_Text optionText = optionButton.GetComponentInChildren<TMP_Text>(true);
            if (optionText != null)
            {
                optionText.text = modelPrefab.name;
                optionTexts[selectedIndex] = optionText;
                optionTextDefaultColors[selectedIndex] = optionText.color;
            }
            else
            {
                Debug.LogWarning(
                    $"The model option Button prefab has no TMP_Text child for '{modelPrefab.name}'.",
                    optionButton);
            }

            if (modelPrefab == currentModelPrefab)
            {
                selectedOptionIndex = selectedIndex;
            }

            optionButton.onClick.AddListener(() => SelectModel(selectedIndex));
            generatedOptions.Add(optionButton.gameObject);
        }

        if (selectedOptionIndex < 0)
        {
            foreach (int optionIndex in optionTexts.Keys)
            {
                selectedOptionIndex = optionIndex;
                break;
            }
        }

        if (selectedOptionIndex >= 0)
        {
            UpdateSelectedOptionTextColor(selectedOptionIndex);
        }

        if (scrollViewContent is RectTransform contentRect)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.MarkLayoutForRebuild(contentRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
    }

    public void SelectModel(int modelIndex)
    {
        if (modelIndex < 0 ||
            modelIndex >= BaseModelPrefabs.Count ||
            BaseModelPrefabs[modelIndex] == null)
        {
            Debug.LogWarning($"Cannot select model at invalid index {modelIndex}.", this);
            return;
        }

        GameObject selectedModelPrefab = BaseModelPrefabs[modelIndex];
        UpdateSelectedOptionTextColor(modelIndex);

        if (selectedModelPrefab == currentModelPrefab)
        {
            return;
        }

        if (CurrentModel != null)
        {
            Destroy(CurrentModel);
        }

        CurrentModel = Instantiate(
            selectedModelPrefab,
            ModelSpawnPosition,
            Quaternion.Euler(ModelSpawnRotation));
        currentModelPrefab = selectedModelPrefab;
        modelPreview?.SetPrefab(CurrentModel);
        diyManager?.RegisterModel(CurrentModel);
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
