using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class MultipleImageTracker : MonoBehaviour, IPrefabInitiationSource
{
    [System.Serializable]
    public class MarkerPrefabPair
    {
        public string markerName;
        public GameObject prefab;

        [Header("Marker-relative Placement")]
        public Vector3 prefabLocalPosition;
        public Vector3 prefabLocalEulerAngles;
        [Tooltip("Final local XYZ scale for this marker's spawned prefab or avatar.")]
        public Vector3 size = Vector3.one;
    }

    private sealed class MarkerContent
    {
        public MarkerPrefabPair Pair;
        public GameObject Root;
        public GameObject Main;
        public bool ManagersInitialized;
    }

    [Header("Image Tracking")]
    [SerializeField] private ARTrackedImageManager trackedImageManager;
    [SerializeField] private List<MarkerPrefabPair> markerPrefabs;
    [SerializeField] private string initiationMarkerName = "robot";
    [SerializeField] private GameObject unActivated;

    [Header("Optional Woo Manager Wiring")]
    [SerializeField] private SessionManager sessionManager;
    [SerializeField] private MainSelectManager mainSelectManager;
    [SerializeField] private ChangeAvatarScriptARMarker changeAvatarScript;

    private static MultipleImageTracker activeInstance;
    private readonly Dictionary<string, MarkerContent> spawnedContent = new();

    public bool IsPrefabInitiated { get; private set; }

    private void Awake()
    {
        if (activeInstance != null && activeInstance != this)
        {
            Debug.LogWarning(
                "A second MultipleImageTracker was disabled to prevent duplicate marker content.",
                this);
            enabled = false;
            return;
        }

        activeInstance = this;

        if (trackedImageManager == null)
        {
            trackedImageManager = FindFirstObjectByType<ARTrackedImageManager>();
        }

        if (mainSelectManager == null)
        {
            mainSelectManager = GetComponent<MainSelectManager>();
        }

        foreach (MarkerPrefabPair pair in markerPrefabs)
        {
            if (string.IsNullOrWhiteSpace(pair.markerName))
            {
                continue;
            }

            if (spawnedContent.ContainsKey(pair.markerName))
            {
                Debug.LogWarning($"Duplicate marker mapping ignored: {pair.markerName}", this);
                continue;
            }

            spawnedContent.Add(pair.markerName, CreateMarkerContent(pair));
        }
    }

    private static MarkerContent CreateMarkerContent(MarkerPrefabPair pair)
    {
        GameObject root = new GameObject($"{pair.markerName} Marker Content");
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;

        GameObject main = null;
        if (pair.prefab != null)
        {
            main = Instantiate(pair.prefab, root.transform);
            main.name = pair.prefab.name;
            main.transform.SetLocalPositionAndRotation(
                pair.prefabLocalPosition,
                Quaternion.Euler(pair.prefabLocalEulerAngles));
            main.transform.localScale = pair.size;
        }

        root.SetActive(false);

        return new MarkerContent
        {
            Pair = pair,
            Root = root,
            Main = main
        };
    }

    private void OnEnable()
    {
        if (trackedImageManager == null)
        {
            Debug.LogError("MultipleImageTracker needs an ARTrackedImageManager.", this);
            return;
        }

        trackedImageManager.trackablesChanged.AddListener(OnTrackablesChanged);
    }

    private void OnDisable()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
        }
    }

    private void OnDestroy()
    {
        if (activeInstance == this)
        {
            activeInstance = null;
        }
    }

    private void OnTrackablesChanged(
        ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            UpdateTrackedImage(trackedImage);
        }

        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            UpdateTrackedImage(trackedImage);
        }

        foreach (KeyValuePair<TrackableId, ARTrackedImage> removedImage in eventArgs.removed)
        {
            HideTrackedImage(removedImage.Value);
        }
    }

    private void UpdateTrackedImage(ARTrackedImage trackedImage)
    {
        if (!TryGetMarkerName(trackedImage, out string markerName))
        {
            return;
        }

        Debug.Log(
            $"Tracked image: {markerName}, " +
            $"ID: {trackedImage.trackableId}, " +
            $"ImageState: {trackedImage.trackingState}, " +
            $"ARSession: {ARSession.state}, " +
            $"NotTrackingReason: {ARSession.notTrackingReason}",
            this);

        if (!spawnedContent.TryGetValue(markerName, out MarkerContent content))
        {
            Debug.LogWarning($"No marker mapping assigned for: {markerName}", this);
            return;
        }

        // Follow the tracked image directly. No stable-pose history or delayed
        // first spawn is used here.
        content.Root.transform.SetParent(trackedImage.transform, false);
        content.Root.transform.SetLocalPositionAndRotation(
            Vector3.zero,
            Quaternion.identity);
        content.Root.transform.localScale = Vector3.one;

        bool isTracking = trackedImage.trackingState == TrackingState.Tracking;
        content.Root.SetActive(isTracking);

        if (!isTracking)
        {
            return;
        }

        if (!content.ManagersInitialized)
        {
            content.ManagersInitialized = InitializeManagers(content);
        }

        if (markerName == initiationMarkerName)
        {
            IsPrefabInitiated = true;
            unActivated?.SetActive(false);
        }
    }

    private void HideTrackedImage(ARTrackedImage trackedImage)
    {
        if (!TryGetMarkerName(trackedImage, out string markerName))
        {
            return;
        }

        if (spawnedContent.TryGetValue(markerName, out MarkerContent content))
        {
            content.Root.SetActive(false);
        }
    }

    private bool InitializeManagers(MarkerContent content)
    {
        if (content.Main != null)
        {
            BubbleClean bubbleClean = content.Main.GetComponentInChildren<BubbleClean>(true);
            mainSelectManager?.SetBubbleClean(bubbleClean);

            PrefabAnimator prefabAnimator =
                content.Main.GetComponentInChildren<PrefabAnimator>(true);
            if (sessionManager != null && prefabAnimator != null)
            {
                sessionManager.bubbleAnimator = prefabAnimator.bubbleAnimator;
            }
        }

        if (content.Pair.markerName == initiationMarkerName &&
            changeAvatarScript != null)
        {
            GameObject avatar = changeAvatarScript.InitializeForMarker(
                content.Root.transform,
                content.Pair.prefabLocalPosition,
                Quaternion.Euler(content.Pair.prefabLocalEulerAngles),
                content.Pair.size,
                sessionManager);

            if (avatar == null)
            {
                Debug.LogWarning(
                    $"Avatar initialization failed for marker '{content.Pair.markerName}'. Retrying.",
                    this);
                return false;
            }
        }

        mainSelectManager?.NotifyPrefabPlaced();
        return true;
    }

    private static bool TryGetMarkerName(
        ARTrackedImage trackedImage,
        out string markerName)
    {
        markerName = trackedImage == null
            ? null
            : trackedImage.referenceImage.name;

        if (!string.IsNullOrWhiteSpace(markerName))
        {
            return true;
        }

        Debug.LogWarning(
            "Ignoring a tracked image that is not matched to the reference image library.");
        return false;
    }
}
