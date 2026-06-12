using UnityEngine;
using System;
using System.Collections.Generic;
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
    public void AddToBubbleVault()
    {
        vault.Add(new BubbleVault{bubbleCreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), 
                                bubbleContent = inputField.text});
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
}
