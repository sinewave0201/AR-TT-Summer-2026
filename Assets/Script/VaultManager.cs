using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;

public class VaultManager : MonoBehaviour
{
    [System.Serializable]
    public struct BubbleVault
    {
        public String bubbleCreatedDate;
        public String bubbleContent;
    }
    
    [Header("bubble Storage")]
    public List<BubbleVault> vault = new List<BubbleVault>();
    public TMP_InputField inputField;

    [Header("UI Rendering")]
    public Transform content;
    public GameObject itemPrefab;

    private const string SaveFileName = "bubble_vault.json";
    private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    [System.Serializable]
    private class BubbleVaultSaveData
    {
        public List<BubbleVault> vault = new List<BubbleVault>();
    }

    void Awake()
    {
        LoadVault();
    }

    public void AddToBubbleVault()
    {
        if (inputField == null)
        {
            Debug.LogError("VaultManager needs an inputField reference.", this);
            return;
        }

        vault.Add(new BubbleVault
        {
            bubbleCreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            bubbleContent = inputField.text
        });

        SaveVault();
        RenderList();
    }

    void OnEnable()
    {
        RenderList();
    }

    void RenderList()
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        foreach (BubbleVault bubble in vault)
        {
            GameObject obj = Instantiate(itemPrefab, content);
            ItemUI itemUI = obj.GetComponent<ItemUI>();
            itemUI.SetData(bubble.bubbleCreatedDate, bubble.bubbleContent);
        }
    }

    private void SaveVault()
    {
        BubbleVaultSaveData saveData = new BubbleVaultSaveData
        {
            vault = vault
        };

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"Bubble vault saved to {SavePath}", this);
    }

    private void LoadVault()
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
        }
    }
}
