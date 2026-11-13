#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class TracePrefabSetup
{
    // Reuse the existing asset path/guid so scene and build references remain stable.
    internal const string OutputPath = "Assets/Prefab/Network/TraceRobot.prefab";
    private const string OldSettingsPath = "Assets/Prefab/TTsettingPrefab.prefab";
    private const string SessionKey = "ARTT.TracePrefabSetup.v3";

    static TracePrefabSetup()
    {
        EditorApplication.delayCall += RunOnce;
    }

    private static void RunOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
            return;
        }

        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);
        BuildTraceSettingPrefab();
    }

    private static void HandlePlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode)
        {
            return;
        }

        EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
        EditorApplication.delayCall += RunOnce;
    }

    [MenuItem("Tools/Thought Trader/Rebuild Trace Setting Prefab")]
    public static void BuildTraceSettingPrefab()
    {
        GameObject oldRoot = PrefabUtility.LoadPrefabContents(OldSettingsPath);
        try
        {
            BubbleBloom oldBloom = oldRoot.GetComponentInChildren<BubbleBloom>(true);
            if (oldBloom == null)
            {
                Debug.LogError("Trace setting setup could not find BubbleBloom.");
                return;
            }

            SerializedObject bloomData = new SerializedObject(oldBloom);
            GameObject bubbleSource = oldBloom.gameObject;
            GameObject potSource = GetObject(bloomData, "pot");
            GameObject flowerSource = GetObject(bloomData, "flower");
            if (potSource == null || flowerSource == null)
            {
                Debug.LogError("TTsettingPrefab is missing BubbleBloom's pot/flower references.");
                return;
            }

            // Keep the old Setting scale. The placement manager supplies the old
            // Setting/Robot position and rotation relationship at runtime.
            GameObject root = new GameObject("TraceRobot");
            try
            {
                root.transform.localScale = oldRoot.transform.localScale;

                GameObject templates = new GameObject("Trace Templates");
                templates.transform.SetParent(root.transform, false);

                GameObject bubble = DuplicateTemplate(
                    bubbleSource, templates.transform, "Bubble Template");
                RemoveLegacyInterfaces(bubble);
                GameObject pot = DuplicateTemplate(
                    potSource, templates.transform, "Pot Template");
                RemoveLegacyInterfaces(pot);
                GameObject flower = DuplicateTemplate(
                    flowerSource, templates.transform, "Flower Template");
                RemoveLegacyInterfaces(flower);
                templates.SetActive(false);

                TraceManager traceManager = root.AddComponent<TraceManager>();
                SerializedObject traceData = new SerializedObject(traceManager);
                traceData.FindProperty("bubbleTemplate").objectReferenceValue = bubble;
                traceData.FindProperty("potTemplate").objectReferenceValue = pot;
                traceData.FindProperty("flowerTemplate").objectReferenceValue = flower;
                traceData.FindProperty("visualOrigin").vector3Value = Vector3.zero;
                traceData.FindProperty("visualScale").vector3Value = Vector3.one;
                traceData.FindProperty("flowerRiseDistance").floatValue = 1f;
                traceData.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, OutputPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Trace setting prefab rebuilt at {OutputPath}.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(oldRoot);
        }
    }

    private static GameObject GetObject(SerializedObject data, string propertyName)
    {
        SerializedProperty property = data.FindProperty(propertyName);
        return property != null ? property.objectReferenceValue as GameObject : null;
    }

    private static GameObject DuplicateTemplate(
        GameObject source,
        Transform parent,
        string name)
    {
        GameObject copy = Object.Instantiate(source);
        copy.name = name;
        copy.transform.SetParent(parent, false);
        copy.transform.localPosition = source.transform.localPosition;
        copy.transform.localRotation = source.transform.localRotation;
        copy.transform.localScale = source.transform.localScale;
        copy.SetActive(false);
        return copy;
    }

    private static void RemoveLegacyInterfaces(GameObject target)
    {
        foreach (MonoBehaviour behaviour in target.GetComponentsInChildren<MonoBehaviour>(true))
        {
            // Remove old project interfaces while keeping rendering components such
            // as TextMeshProUGUI, which is also a MonoBehaviour.
            if (behaviour != null &&
                behaviour.GetType().Assembly == typeof(BubbleBloom).Assembly)
            {
                Object.DestroyImmediate(behaviour);
            }
        }
    }
}
#endif
