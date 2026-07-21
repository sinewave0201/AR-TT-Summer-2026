using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine.UI;

public class VaultManager : MonoBehaviour
{
    public event Action VaultChanged;

    [System.Serializable]
    public struct BubbleVault
    {
        public String bubbleCreatedDate;
        public String bubbleContent;
        public List<String>bubbleEmotion;//storable emotion option
    }
    
    [Header("bubble Storage")]
    public List<BubbleVault> vault = new List<BubbleVault>();
    public TMP_InputField inputField;

    [Header("UI Rendering")]
    public Transform content;
    public GameObject itemPrefab;
    private Coroutine rebuildLayoutCoroutine;

    private const string SaveFileName = "bubble_vault.json";
    private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    [System.Serializable]
    private class BubbleVaultSaveData
    {
        public List<BubbleVault> vault = new List<BubbleVault>();
    }

    [Header("Current Bubble Vault")]
    private BubbleVault curBubbleVault;

    void Awake()
    {
        LoadVault();
    }

    //call whenever a session starts to initialize the current bubble vault
    public void InitializeCurBV()
    {
        curBubbleVault = new BubbleVault
        {
            bubbleEmotion = new List<string>()
        };
    }

    public void AddEmoToCurVault(String emo)
    {
        curBubbleVault.bubbleEmotion.Add(emo);
        Debug.Log($"Emotion added{String.Join(", ", curBubbleVault.bubbleEmotion)}");
    }

    public void AddToBubbleVault()
    {
        if (inputField == null)
        {
            Debug.LogError("VaultManager needs an inputField reference.", this);
            return;
        }

        curBubbleVault.bubbleCreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        curBubbleVault.bubbleContent = inputField.text;

        vault.Add(curBubbleVault);

        SaveVault();
        RenderList();
        VaultChanged?.Invoke();
    }

    public void AIAddToBubbleVault(string bubbleContent)
    {
        vault.Add(new BubbleVault
        {
            bubbleCreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            bubbleContent = bubbleContent
        });

        SaveVault();
        RenderList();
        VaultChanged?.Invoke();
    }

    void OnEnable()
    {
        RenderList();
    }

    void RenderList()
    {
        if (content == null)
        {
            Debug.LogError("VaultManager needs a content reference.", this);
            return;
        }

        if (itemPrefab == null)
        {
            Debug.LogError("VaultManager needs an itemPrefab reference.", this);
            return;
        }

        foreach (Transform child in content)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        foreach (BubbleVault bubble in vault)
        {
            GameObject obj = Instantiate(itemPrefab, content);
            ItemUI itemUI = obj.GetComponent<ItemUI>();
            if (itemUI == null)
            {
                Debug.LogError("Vault itemPrefab needs an ItemUI component.", obj);
                continue;
            }

            itemUI.SetData(bubble.bubbleCreatedDate, bubble.bubbleContent, bubble.bubbleEmotion);
        }

        if (rebuildLayoutCoroutine != null)
        {
            StopCoroutine(rebuildLayoutCoroutine);
        }

        rebuildLayoutCoroutine = StartCoroutine(RebuildLayoutNextFrame());
    }

    private IEnumerator RebuildLayoutNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        foreach (Transform child in content)
        {
            if (!child.gameObject.activeSelf)
            {
                continue;
            }

            if (child.TryGetComponent(out ItemUI itemUI))
            {
                itemUI.RefreshLayout();
            }
        }

        if (content is RectTransform contentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        Canvas.ForceUpdateCanvases();
        rebuildLayoutCoroutine = null;
    }

    private void SaveVault()
    {
        BubbleVaultSaveData saveData = new BubbleVaultSaveData
        {
            vault = vault
        };

        string json = JsonUtility.ToJson(saveData, true);
        try
        {
            File.WriteAllText(SavePath, json);
            Debug.Log($"Bubble vault saved to {SavePath}", this);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to save bubble vault to {SavePath}: {exception.Message}", this);
        }
    }

    private void LoadVault()
    {
        try
        {
            if (!File.Exists(SavePath))
            {
                return;
            }

            string json = File.ReadAllText(SavePath);
            BubbleVaultSaveData saveData = JsonUtility.FromJson<BubbleVaultSaveData>(json);

            if (saveData != null && saveData.vault != null)
            {
                vault = saveData.vault;
                VaultChanged?.Invoke();
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to load bubble vault from {SavePath}: {exception.Message}", this);
        }
    }
}
