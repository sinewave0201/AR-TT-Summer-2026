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
        public Vector3 potLocalPosition;
    }

    [Serializable]
    private class TraceSaveData
    {
        public List<TraceRecord> traces = new List<TraceRecord>();
    }

    private sealed class PendingTrace
    {
        public string id;
        public string createdAt;
        public Vector3 targetPotLocalPosition;
        public GameObject group;
        public GameObject bubble;
        public GameObject pot;
        public GameObject flower;
    }

    [Header("Templates copied from TTsettingPrefab")]
    [SerializeField] private GameObject bubbleTemplate;
    [SerializeField] private GameObject potTemplate;
    [SerializeField] private GameObject flowerTemplate;

    [Header("Animation")]
    [SerializeField] private AnimationManager animationManager;
    [SerializeField, Min(0.1f)] private float flowerGrowthSeconds = 5f;
    [SerializeField] private Vector3 grownFlowerLocalPosition =
        new Vector3(0.734f, 0.135f, -2.382f);

    [Header("Completed trace placement")]
    [SerializeField, Min(0f)] private float minimumRadius = 1.5f;
    [SerializeField, Min(0.1f)] private float maximumRadius = 3.5f;
    [SerializeField, Min(0f)] private float minimumPotSpacing = 0.65f;

    private const string SaveFileName = "trace_vault.json";
    private const string LegacySaveFileName = "bubble_vault.json";
    private const string BubbleAppearTrigger = "Appear";
    private const string BubbleBloomState = "Bloom";
    private const string FlowerBloomState = "Armature|Bloom";

    private readonly Dictionary<string, PendingTrace> pending =
        new Dictionary<string, PendingTrace>();
    private readonly HashSet<string> completing = new HashSet<string>();
    private TraceSaveData saveData = new TraceSaveData();
    private GameObject manualBubble;
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
        RestoreCompletedTraces();
    }

    public bool BeginTrace(string traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId) || pending.Count > 0)
        {
            return false;
        }

        if (!TemplatesAreValid())
        {
            return false;
        }

        PendingTrace trace = CreateVisuals(traceId);
        trace.createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        trace.targetPotLocalPosition = ChooseSavedPosition();
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

    public void ShowBubbleForGetAnimation()
    {
        foreach (PendingTrace trace in pending.Values)
        {
            ShowBubble(trace.bubble);
            return;
        }

        if (bubbleTemplate == null)
        {
            Debug.LogWarning("GetBubble cannot show a bubble: template is missing.", this);
            return;
        }

        if (manualBubble == null)
        {
            manualBubble = InstantiateTemplate(bubbleTemplate, transform, "GetBubble Preview");
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

    private PendingTrace CreateVisuals(string traceId)
    {
        GameObject group = new GameObject($"Pending Trace {traceId}");
        group.transform.SetParent(transform, false);

        GameObject bubble = InstantiateTemplate(
            bubbleTemplate,
            transform,
            "Trace Bubble");
        GameObject pot = InstantiateTemplate(potTemplate, group.transform, "Trace Pot");
        GameObject flower = InstantiateTemplate(
            flowerTemplate,
            group.transform,
            "Trace Flower");

        pot.SetActive(true);
        flower.SetActive(false);
        bubble.SetActive(true);

        return new PendingTrace
        {
            id = traceId,
            group = group,
            bubble = bubble,
            pot = pot,
            flower = flower
        };
    }

    private IEnumerator CompleteSequence(PendingTrace trace, string content)
    {
        SetBubbleText(trace.bubble, content);

        Animator bubbleAnimator = trace.bubble.GetComponentInChildren<Animator>(true);
        if (bubbleAnimator != null)
        {
            bubbleAnimator.enabled = true;
            bubbleAnimator.Play(BubbleBloomState, 0, 0f);
            bubbleAnimator.Update(0f);
            yield return WaitForAnimation(bubbleAnimator, BubbleBloomState, 10f);
        }

        if (trace.bubble != null)
        {
            Destroy(trace.bubble);
        }

        trace.flower.SetActive(true);
        Vector3 flowerStart = trace.flower.transform.localPosition;
        Animator flowerAnimator = trace.flower.GetComponentInChildren<Animator>(true);
        if (flowerAnimator != null)
        {
            flowerAnimator.enabled = true;
            flowerAnimator.speed = 1f;
            flowerAnimator.Play(FlowerBloomState, 0, 0f);
        }

        float elapsed = 0f;
        while (elapsed < flowerGrowthSeconds)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / flowerGrowthSeconds));
            trace.flower.transform.localPosition = Vector3.Lerp(
                flowerStart,
                grownFlowerLocalPosition,
                progress);
            yield return null;
        }

        if (flowerAnimator != null)
        {
            flowerAnimator.speed = 0f;
        }

        MoveGroupSoPotIsAt(trace, trace.targetPotLocalPosition);

        saveData.traces.Add(new TraceRecord
        {
            id = trace.id,
            createdAt = trace.createdAt,
            content = content,
            potLocalPosition = trace.targetPotLocalPosition
        });
        Save();
        completing.Remove(trace.id);
        pending.Remove(trace.id);
    }

    private void RestoreCompletedTraces()
    {
        if (!TemplatesAreValid())
        {
            return;
        }

        foreach (TraceRecord record in saveData.traces)
        {
            GameObject group = new GameObject($"Saved Trace {record.id}");
            group.transform.SetParent(transform, false);
            GameObject pot = InstantiateTemplate(potTemplate, group.transform, "Trace Pot");
            GameObject flower = InstantiateTemplate(
                flowerTemplate,
                group.transform,
                "Trace Flower");

            pot.SetActive(true);
            flower.SetActive(true);
            flower.transform.localPosition = grownFlowerLocalPosition;

            Animator flowerAnimator = flower.GetComponentInChildren<Animator>(true);
            if (flowerAnimator != null)
            {
                flowerAnimator.enabled = true;
                flowerAnimator.Play(FlowerBloomState, 0, 1f);
                flowerAnimator.Update(0f);
                flowerAnimator.speed = 0f;
            }

            PendingTrace restored = new PendingTrace
            {
                group = group,
                pot = pot,
                flower = flower
            };
            MoveGroupSoPotIsAt(restored, record.potLocalPosition);
        }
    }

    private void MoveGroupSoPotIsAt(PendingTrace trace, Vector3 targetPotPosition)
    {
        Vector3 currentPotPosition =
            trace.group.transform.TransformPoint(trace.pot.transform.localPosition);
        Vector3 targetWorldPosition = transform.TransformPoint(targetPotPosition);
        trace.group.transform.position += targetWorldPosition - currentPotPosition;
    }

    private Vector3 ChooseSavedPosition()
    {
        float maxRadius = Mathf.Max(minimumRadius, maximumRadius);
        for (int attempt = 0; attempt < 24; attempt++)
        {
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float radius = UnityEngine.Random.Range(minimumRadius, maxRadius);
            Vector3 candidate = new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius);

            bool overlaps = false;
            foreach (TraceRecord record in saveData.traces)
            {
                Vector2 difference = new Vector2(
                    candidate.x - record.potLocalPosition.x,
                    candidate.z - record.potLocalPosition.z);
                if (difference.sqrMagnitude < minimumPotSpacing * minimumPotSpacing)
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
            {
                return candidate;
            }
        }

        float fallbackAngle = saveData.traces.Count * 2.39996323f;
        return new Vector3(
            Mathf.Cos(fallbackAngle) * maxRadius,
            0f,
            Mathf.Sin(fallbackAngle) * maxRadius);
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
        animator.ResetTrigger(BubbleAppearTrigger);
        animator.SetTrigger(BubbleAppearTrigger);
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
                if (!animator.IsInTransition(0) && state.normalizedTime >= 1f)
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

    private bool TemplatesAreValid()
    {
        if (bubbleTemplate != null && potTemplate != null && flowerTemplate != null)
        {
            return true;
        }

        Debug.LogError(
            "TraceManager needs Bubble, Pot, and Flower templates from TTsettingPrefab.",
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
