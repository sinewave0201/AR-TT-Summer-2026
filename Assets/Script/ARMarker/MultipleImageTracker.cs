using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class MultipleImageTracker : MonoBehaviour
{
    [System.Serializable]
    public class MarkerPrefabPair
    {
        public string markerName;
        public GameObject prefab;
    }

    [SerializeField]
    private ARTrackedImageManager trackedImageManager;

    [SerializeField]
    private List<MarkerPrefabPair> markerPrefabs;

    private readonly Dictionary<string, GameObject> spawnedObjects = new();

    private void Awake()
    {
        foreach (MarkerPrefabPair pair in markerPrefabs)
        {
            if (pair.prefab == null || string.IsNullOrWhiteSpace(pair.markerName))
                continue;

            GameObject spawned = Instantiate(pair.prefab);
            spawned.name = pair.prefab.name;
            spawned.SetActive(false);

            spawnedObjects[pair.markerName] = spawned;
        }
    }

    private void OnEnable()
    {
        trackedImageManager.trackablesChanged.AddListener(OnTrackablesChanged);
    }

    private void OnDisable()
    {
        trackedImageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
    }

    private void OnTrackablesChanged(
        ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (ARTrackedImage trackedImage in eventArgs.added)
            UpdateTrackedImage(trackedImage);

        foreach (ARTrackedImage trackedImage in eventArgs.updated)
            UpdateTrackedImage(trackedImage);

        foreach (KeyValuePair<TrackableId, ARTrackedImage> removedImage
                 in eventArgs.removed)
        {
            HideTrackedImage(removedImage.Value);
        }
    }

    private void UpdateTrackedImage(ARTrackedImage trackedImage)
    {
        if (!TryGetMarkerName(trackedImage, out string markerName))
            return;

        Debug.Log(
            $"Tracked image: {markerName}, " +
            $"ID: {trackedImage.trackableId}, " +
            $"State: {trackedImage.trackingState}"
        );

        if (!spawnedObjects.TryGetValue(markerName, out GameObject spawned))
        {
            Debug.LogWarning($"No prefab assigned for marker: {markerName}");
            return;
        }

        spawned.transform.SetPositionAndRotation(
            trackedImage.transform.position,
            trackedImage.transform.rotation
        );

        // Make the prefab follow the marker.
        spawned.transform.SetParent(trackedImage.transform, true);

        bool isTracking =
            trackedImage.trackingState == TrackingState.Tracking;

        spawned.SetActive(isTracking);
    }

    private void HideTrackedImage(ARTrackedImage trackedImage)
    {
        if (!TryGetMarkerName(trackedImage, out string markerName))
            return;

        if (spawnedObjects.TryGetValue(markerName, out GameObject spawned))
            spawned.SetActive(false);
    }

    private static bool TryGetMarkerName(
        ARTrackedImage trackedImage,
        out string markerName)
    {
        markerName = trackedImage == null
            ? null
            : trackedImage.referenceImage.name;

        if (!string.IsNullOrWhiteSpace(markerName))
            return true;

        Debug.LogWarning(
            "Ignoring a tracked image that is not matched to the " +
            "reference image library."
        );
        return false;
    }
}
