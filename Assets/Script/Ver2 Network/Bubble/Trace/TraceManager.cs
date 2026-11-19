using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class TraceManager : MonoBehaviour
{
    [Serializable]
    public class TraceRecord
    {
        public string id;
        public string createdAt;
        public string content;
    }

    [Serializable]
    private class TraceSaveData
    {
        public int version = CurrentSaveVersion;
        public List<TraceRecord> traces = new List<TraceRecord>();
    }

    private sealed class PendingTrace
    {
        public string id;
        public string createdAt;
        public GameObject group;
        public GameObject bubble;
    }

    [Header("Bubble template")]
    [SerializeField] private GameObject bubbleTemplate;
    [SerializeField] private Vector3 visualOrigin;
    [SerializeField] private Vector3 visualScale = Vector3.one;

    [Header("Animation")]
    [SerializeField] private AnimationManager animationManager;

    [Header("Destroy effect")]
    [SerializeField] private GameObject destroyEffect;
    [SerializeField, Min(0.1f)] private float destroyEffectLifetime = 6f;

    private const string SaveFileName = "trace_vault.json";
    private const string LegacySaveFileName = "bubble_vault.json";
    private const int CurrentSaveVersion = 3;
    private const string BubbleAppearState = "Appear";
    private const string BubbleBloomState = "Bloom";

    private readonly Dictionary<string, PendingTrace> pending =
        new Dictionary<string, PendingTrace>();
    private readonly HashSet<string> completing = new HashSet<string>();
    private TraceSaveData saveData = new TraceSaveData();
    private GameObject manualBubble;
    private GameObject manualBubbleGroup;
    private Coroutine manualBubbleRoutine;

    private string SavePath =>
        Path.Combine(Application.persistentDataPath, SaveFileName);
    private string LegacySavePath =>
        Path.Combine(Application.persistentDataPath, LegacySaveFileName);

    private void Awake()
    {
        if (animationManager == null)
        {
            animationManager = GetComponentInChildren<AnimationManager>(true);
        }

        ClearLegacyVault();
        Load();
    }

    public void Initialize(AnimationManager hostAnimationManager)
    {
        animationManager = hostAnimationManager;
        animationManager?.SetTraceManager(this);
    }

    public bool BeginTrace(string traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId) ||
            pending.Count > 0 ||
            !TemplateIsValid())
        {
            return false;
        }

        PendingTrace trace = CreateVisual(traceId);
        trace.createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        pending.Add(traceId, trace);

        animationManager?.Play(AnimationManager.RobotAnimation.GetBubble);
        ShowBubble(trace.bubble);
        return true;
    }

    public bool CompleteTrace(string traceId, string content)
    {
        if (!pending.TryGetValue(traceId, out PendingTrace trace) ||
            !completing.Add(traceId))
        {
            return false;
        }

        StartCoroutine(CompleteSequence(trace, content ?? string.Empty));
        return true;
    }

    public bool DestroyTrace(string traceId)
    {
        if (!pending.TryGetValue(traceId, out PendingTrace trace) ||
            completing.Contains(traceId))
        {
            return false;
        }

        PlayDestroyEffect(trace.bubble);
        if (trace.bubble != null)
        {
            trace.bubble.SetActive(false);
        }

        pending.Remove(traceId);
        if (trace.group != null)
        {
            Destroy(trace.group);
        }
        return true;
    }

    public void ShowBubbleForGetAnimation()
    {
        foreach (PendingTrace trace in pending.Values)
        {
            if (!completing.Contains(trace.id))
            {
                ShowBubble(trace.bubble);
            }
            return;
        }

        if (!TemplateIsValid())
        {
            return;
        }

        if (manualBubble == null)
        {
            manualBubbleGroup = new GameObject("GetBubble Preview Group");
            manualBubbleGroup.transform.SetParent(transform, false);
            ApplyVisualFrame(manualBubbleGroup.transform);
            manualBubble = InstantiateTemplate(
                bubbleTemplate,
                manualBubbleGroup.transform,
                "GetBubble Preview");
        }

        ShowBubble(manualBubble);
        if (manualBubbleRoutine != null)
        {
            StopCoroutine(manualBubbleRoutine);
        }
        manualBubbleRoutine = StartCoroutine(HideManualBubbleAfterDelay());
    }

    public string ExportHistoryJson()
    {
        return JsonUtility.ToJson(saveData);
    }

    public bool DeleteTrace(string traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId) || completing.Contains(traceId))
        {
            return false;
        }

        int removed = saveData.traces.RemoveAll(record => record.id == traceId);
        if (removed == 0)
        {
            return false;
        }

        Save();
        return true;
    }

    private PendingTrace CreateVisual(string traceId)
    {
        GameObject group = new GameObject($"Pending Trace {traceId}");
        group.transform.SetParent(transform, false);
        ApplyVisualFrame(group.transform);

        GameObject bubble = InstantiateTemplate(
            bubbleTemplate,
            group.transform,
            "Trace Bubble");
        bubble.SetActive(true);

        return new PendingTrace
        {
            id = traceId,
            group = group,
            bubble = bubble
        };
    }

    private IEnumerator CompleteSequence(PendingTrace trace, string content)
    {
        SetBubbleText(trace.bubble, content);

        Animator bubbleAnimator =
            trace.bubble.GetComponentInChildren<Animator>(true);
        if (bubbleAnimator != null)
        {
            bubbleAnimator.enabled = true;
            yield return WaitForAnimation(
                bubbleAnimator,
                BubbleAppearState,
                10f);

            bubbleAnimator.Play(BubbleBloomState, 0, 0f);
            bubbleAnimator.Update(0f);
            yield return WaitForAnimation(
                bubbleAnimator,
                BubbleBloomState,
                10f);
        }

        saveData.traces.Add(new TraceRecord
        {
            id = trace.id,
            createdAt = trace.createdAt,
            content = content
        });
        Save();

        completing.Remove(trace.id);
        pending.Remove(trace.id);
        if (trace.group != null)
        {
            Destroy(trace.group);
        }
    }

    private void ApplyVisualFrame(Transform target)
    {
        target.localPosition = visualOrigin;
        target.localRotation = Quaternion.identity;
        target.localScale = visualScale;
    }

    private void PlayDestroyEffect(GameObject bubble)
    {
        if (destroyEffect == null)
        {
            Debug.LogWarning(
                "TraceManager needs a Destroy Effect GameObject reference.",
                this);
            return;
        }

        GameObject effect = Instantiate(
            destroyEffect,
            bubble != null ? bubble.transform.position : transform.position,
            destroyEffect.transform.rotation,
            transform);
        effect.SetActive(true);

        foreach (ParticleSystem particle in
                 effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            particle.gameObject.SetActive(true);
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Clear(true);
            particle.Play(true);
        }

        Destroy(effect, destroyEffectLifetime);
    }

    private static GameObject InstantiateTemplate(
        GameObject template,
        Transform parent,
        string instanceName)
    {
        bool wasActive = template.activeSelf;
        template.SetActive(false);
        GameObject instance = Instantiate(template, parent, false);
        template.SetActive(wasActive);
        instance.name = instanceName;
        instance.transform.localPosition = template.transform.localPosition;
        instance.transform.localRotation = template.transform.localRotation;
        instance.transform.localScale = template.transform.localScale;
        return instance;
    }

    private static void ShowBubble(GameObject bubble)
    {
        if (bubble == null)
        {
            return;
        }

        bubble.SetActive(true);
        Animator animator = bubble.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            return;
        }

        animator.enabled = true;
        animator.SetBool("End", false);
        animator.Play(BubbleAppearState, 0, 0f);
        animator.Update(0f);
    }

    private static void SetBubbleText(GameObject bubble, string content)
    {
        TMP_Text text = bubble != null
            ? bubble.GetComponentInChildren<TMP_Text>(true)
            : null;
        if (text != null)
        {
            text.text = content;
        }
    }

    private static IEnumerator WaitForAnimation(
        Animator animator,
        string stateName,
        float timeout)
    {
        float elapsed = 0f;
        bool entered = false;
        while (animator != null && elapsed < timeout)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(stateName))
            {
                entered = true;
                if (!animator.IsInTransition(0) &&
                    state.normalizedTime >= 1f)
                {
                    yield break;
                }
            }
            else if (entered)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator HideManualBubbleAfterDelay()
    {
        yield return new WaitForSeconds(5f);
        if (manualBubble != null)
        {
            manualBubble.SetActive(false);
        }
        manualBubbleRoutine = null;
    }

    private bool TemplateIsValid()
    {
        if (bubbleTemplate != null)
        {
            return true;
        }

        Debug.LogError(
            "TraceManager needs a Bubble template.",
            this);
        return false;
    }

    private void Save()
    {
        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(saveData, true));
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to save traces: {exception.Message}", this);
        }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(SavePath))
            {
                saveData = new TraceSaveData();
                return;
            }

            TraceSaveData loaded =
                JsonUtility.FromJson<TraceSaveData>(File.ReadAllText(SavePath));
            saveData = loaded ?? new TraceSaveData();
            saveData.traces ??= new List<TraceRecord>();

            if (saveData.version != CurrentSaveVersion)
            {
                saveData = new TraceSaveData();
                Save();
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to load traces: {exception.Message}", this);
            saveData = new TraceSaveData();
        }
    }

    private void ClearLegacyVault()
    {
        try
        {
            File.WriteAllText(LegacySavePath, "{\n  \"vault\": []\n}");
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Could not clear the legacy bubble vault: {exception.Message}",
                this);
        }
    }
}
