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
        public int version = CurrentSaveVersion;
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
    [SerializeField] private Vector3 visualOrigin;
    [SerializeField] private Vector3 visualScale = Vector3.one;

    [Header("Animation")]
    [SerializeField] private AnimationManager animationManager;
    [SerializeField, Min(0.1f)] private float flowerGrowthSeconds = 5f;
    [SerializeField, Min(0f)] private float flowerRiseDistance = 1f;

    [Header("Completed trace placement")]
    [SerializeField] private BoxCollider placementArea;
    [SerializeField, Min(0f)] private float minimumPotSpacing = 0.0975f;

    private const string SaveFileName = "trace_vault.json";
    private const string LegacySaveFileName = "bubble_vault.json";
    private const int CurrentSaveVersion = 3;
    private const string BubbleAppearState = "Appear";
    private const string BubbleBloomState = "Bloom";
    private const string FlowerBloomState = "Armature|Bloom";

    private readonly Dictionary<string, PendingTrace> pending =
        new Dictionary<string, PendingTrace>();
    private readonly Dictionary<string, GameObject> completedVisuals =
        new Dictionary<string, GameObject>();
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
        RestoreCompletedTraces();
    }

    public void Initialize(AnimationManager hostAnimationManager)
    {
        animationManager = hostAnimationManager;
        animationManager?.SetTraceManager(this);
    }

    public bool BeginTrace(string traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId) || pending.Count > 0)
        {
            return false;
        }

        if (!TemplatesAreValid() || !PlacementAreaIsValid())
        {
            return false;
        }

        PendingTrace trace = CreateVisuals(traceId);
        trace.createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        trace.targetPotLocalPosition = ChooseSavedPosition(trace.pot);
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
            // CompleteSequence owns the bubble Animator once completion starts.
            // Do not let a delayed GetBubble animation event restart Appear and
            // interrupt Bloom.
            if (!completing.Contains(trace.id))
            {
                ShowBubble(trace.bubble);
            }
            return;
        }

        if (bubbleTemplate == null)
        {
            Debug.LogWarning("GetBubble cannot show a bubble: template is missing.", this);
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

        if (completedVisuals.TryGetValue(traceId, out GameObject visual))
        {
            completedVisuals.Remove(traceId);
            if (visual != null)
            {
                Destroy(visual);
            }
        }

        Save();
        return true;
    }

    private PendingTrace CreateVisuals(string traceId)
    {
        GameObject group = new GameObject($"Pending Trace {traceId}");
        group.transform.SetParent(transform, false);
        ApplyVisualFrame(group.transform);

        GameObject bubble = InstantiateTemplate(
            bubbleTemplate,
            group.transform,
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
            yield return WaitForAnimation(
                bubbleAnimator,
                BubbleAppearState,
                10f);
            bubbleAnimator.Play(BubbleBloomState, 0, 0f);
            bubbleAnimator.Update(0f);
            yield return WaitForAnimation(bubbleAnimator, BubbleBloomState, 10f);
        }

        if (trace.bubble != null)
        {
            Destroy(trace.bubble);
        }

        trace.flower.SetActive(true);
        Animator flowerAnimator = trace.flower.GetComponentInChildren<Animator>(true);
        if (flowerAnimator != null)
        {
            flowerAnimator.enabled = false;
        }

        Vector3 flowerStart = trace.flower.transform.localPosition;
        Vector3 flowerEnd = flowerStart + Vector3.up * flowerRiseDistance;
        float elapsed = 0f;
        while (elapsed < flowerGrowthSeconds)
        {
            elapsed += Time.deltaTime;
            float animationProgress = Mathf.Clamp01(elapsed / flowerGrowthSeconds);
            float progress = Mathf.SmoothStep(
                0f,
                1f,
                animationProgress);
            trace.flower.transform.localPosition = Vector3.Lerp(
                flowerStart,
                flowerEnd,
                progress);
            yield return null;
        }

        trace.flower.transform.localPosition = flowerEnd;
        if (flowerAnimator != null)
        {
            flowerAnimator.enabled = true;
            flowerAnimator.speed = 1f;
            flowerAnimator.Play(FlowerBloomState, 0, 0f);
            flowerAnimator.Update(0f);
            yield return WaitForAnimation(flowerAnimator, FlowerBloomState, 10f);
            HoldFlowerOpen(flowerAnimator);
        }

        MoveGroupSoPotIsAt(trace, trace.targetPotLocalPosition);

        saveData.traces.Add(new TraceRecord
        {
            id = trace.id,
            createdAt = trace.createdAt,
            content = content,
            potLocalPosition = trace.targetPotLocalPosition
        });
        completedVisuals[trace.id] = trace.group;
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
            ApplyVisualFrame(group.transform);
            GameObject pot = InstantiateTemplate(potTemplate, group.transform, "Trace Pot");
            GameObject flower = InstantiateTemplate(
                flowerTemplate,
                group.transform,
                "Trace Flower");

            pot.SetActive(true);
            flower.SetActive(true);
            flower.transform.localPosition += Vector3.up * flowerRiseDistance;

            Animator flowerAnimator = flower.GetComponentInChildren<Animator>(true);
            HoldFlowerOpen(flowerAnimator);

            PendingTrace restored = new PendingTrace
            {
                group = group,
                pot = pot,
                flower = flower
            };
            MoveGroupSoPotIsAt(restored, record.potLocalPosition);
            completedVisuals[record.id] = group;
        }
    }

    private void MoveGroupSoPotIsAt(PendingTrace trace, Vector3 targetPotPosition)
    {
        Vector3 currentPotPosition =
            trace.group.transform.TransformPoint(trace.pot.transform.localPosition);
        Vector3 targetWorldPosition = transform.TransformPoint(targetPotPosition);
        trace.group.transform.position += targetWorldPosition - currentPotPosition;
    }

    private void ApplyVisualFrame(Transform target)
    {
        target.localPosition = visualOrigin;
        target.localRotation = Quaternion.identity;
        target.localScale = visualScale;
    }

    private static void HoldFlowerOpen(Animator flowerAnimator)
    {
        if (flowerAnimator == null)
        {
            return;
        }

        flowerAnimator.enabled = true;
        flowerAnimator.keepAnimatorStateOnDisable = true;
        flowerAnimator.speed = 1f;
        flowerAnimator.Play(FlowerBloomState, 0, 0.999f);
        flowerAnimator.Update(0f);
        flowerAnimator.enabled = false;
    }

    private Vector3 ChooseSavedPosition(GameObject pot)
    {
        Vector3 bestCandidate = Vector3.zero;
        float bestClearanceSquared = -1f;

        for (int attempt = 0; attempt < 24; attempt++)
        {
            Vector3 candidate = ChoosePositionInPlacementArea(pot);

            bool overlaps = false;
            float nearestPotSquared = float.PositiveInfinity;
            foreach (TraceRecord record in saveData.traces)
            {
                Vector2 difference = new Vector2(
                    candidate.x - record.potLocalPosition.x,
                    candidate.z - record.potLocalPosition.z);
                float distanceSquared = difference.sqrMagnitude;
                nearestPotSquared = Mathf.Min(nearestPotSquared, distanceSquared);
                if (distanceSquared < minimumPotSpacing * minimumPotSpacing)
                {
                    overlaps = true;
                }
            }

            if (!overlaps)
            {
                return candidate;
            }

            if (nearestPotSquared > bestClearanceSquared)
            {
                bestCandidate = candidate;
                bestClearanceSquared = nearestPotSquared;
            }
        }

        // When the box is crowded, remain inside it and use the sampled point
        // with the most clearance.
        return bestCandidate;
    }

    private Vector3 ChoosePositionInPlacementArea(GameObject pot)
    {
        Vector3 center = placementArea.center;
        Vector3 halfSize = placementArea.size * 0.5f;
        GetPotBoundsOffsetsInPlacementSpace(
            pot,
            out Vector3 minimumOffset,
            out Vector3 maximumOffset);

        float minimumX = center.x - halfSize.x - minimumOffset.x;
        float maximumX = center.x + halfSize.x - maximumOffset.x;
        float minimumZ = center.z - halfSize.z - minimumOffset.z;
        float maximumZ = center.z + halfSize.z - maximumOffset.z;

        if (minimumX > maximumX || minimumZ > maximumZ)
        {
            Debug.LogError(
                "The pot is too large to fit completely inside the Placement Area.",
                this);
            minimumX = maximumX = center.x;
            minimumZ = maximumZ = center.z;
        }

        Vector3 areaLocalPosition = new Vector3(
            UnityEngine.Random.Range(minimumX, maximumX),
            center.y - halfSize.y - minimumOffset.y,
            UnityEngine.Random.Range(minimumZ, maximumZ));

        Vector3 worldPosition =
            placementArea.transform.TransformPoint(areaLocalPosition);
        return transform.InverseTransformPoint(worldPosition);
    }

    private void GetPotBoundsOffsetsInPlacementSpace(
        GameObject pot,
        out Vector3 minimumOffset,
        out Vector3 maximumOffset)
    {
        Vector3 potPivot = placementArea.transform.InverseTransformPoint(
            pot.transform.position);
        minimumOffset = Vector3.zero;
        maximumOffset = Vector3.zero;

        Renderer[] renderers = pot.GetComponentsInChildren<Renderer>(true);
        bool foundBounds = false;
        foreach (Renderer renderer in renderers)
        {
            Bounds bounds = renderer.bounds;
            Vector3 boundsMinimum = bounds.min;
            Vector3 boundsMaximum = bounds.max;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 worldCorner = new Vector3(
                    (corner & 1) == 0 ? boundsMinimum.x : boundsMaximum.x,
                    (corner & 2) == 0 ? boundsMinimum.y : boundsMaximum.y,
                    (corner & 4) == 0 ? boundsMinimum.z : boundsMaximum.z);
                Vector3 offset = placementArea.transform.InverseTransformPoint(
                    worldCorner) - potPivot;

                if (!foundBounds)
                {
                    minimumOffset = maximumOffset = offset;
                    foundBounds = true;
                }
                else
                {
                    minimumOffset = Vector3.Min(minimumOffset, offset);
                    maximumOffset = Vector3.Max(maximumOffset, offset);
                }
            }
        }

        if (!foundBounds)
        {
            Debug.LogWarning(
                "The pot has no Renderer, so placement uses its pivot as its bounds.",
                pot);
        }
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

    private bool PlacementAreaIsValid()
    {
        if (placementArea != null)
        {
            return true;
        }

        Debug.LogError(
            "TraceManager needs a Placement Area BoxCollider.",
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
