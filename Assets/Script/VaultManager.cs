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

    
    public void AddToBubbleVault()
    {
        vault.Add(new BubbleVault{bubbleCreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), 
                                bubbleContent = inputField.text});
    }
}
