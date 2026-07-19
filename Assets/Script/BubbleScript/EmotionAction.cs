using System.Collections.Generic; // List
using UnityEngine;
using UnityEngine.Events;        // UnityEvent

public class EmotionAction : MonoBehaviour
{
    [SerializeField] private UnityEvent beforeDestroy;

    public void InvokeBeforeDestroy()
    {
        beforeDestroy?.Invoke();
    }
}