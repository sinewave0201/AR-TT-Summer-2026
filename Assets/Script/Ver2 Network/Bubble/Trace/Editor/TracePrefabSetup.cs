#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class TracePrefabSetup
{
    internal const string OutputPath = "Assets/Prefab/Network/TraceRobot.prefab";
    private const string OldSettingsPath = "Assets/Prefab/TTsettingPrefab.prefab";
    private const string DestroyEffectPath =
        "Assets/Art/FreeQuickEffectsVol1/Prefabs/vfx_Impact_01.prefab";
    private const string SessionKey = "ARTT.TracePrefabSetup.v4";

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
            BubbleBloom oldBloom =
                oldRoot.GetComponentInChildren<BubbleBloom>(true);
            if (oldBloom == null)
            {
                Debug.LogError(
                    "Trace setting setup could not find BubbleBloom.");
                return;
            }

            TraceManager oldTraceManager =
                oldRoot.GetComponentInChildren<TraceManager>(true);
            SerializedObject oldTraceData = oldTraceManager != null
                ? new SerializedObject(oldTraceManager)
                : null;
            GameObject destroyEffect = oldTraceData?
                .FindProperty("destroyEffect")?.objectReferenceValue as GameObject;
            destroyEffect ??=
                AssetDatabase.LoadAssetAtPath<GameObject>(DestroyEffectPath);

            GameObject root = new GameObject("TraceRobot");
            try
            {
                root.transform.localScale = oldRoot.transform.localScale;

                GameObject templates = new GameObject("Trace Templates");
                templates.transform.SetParent(root.transform, false);

                GameObject bubble = DuplicateTemplate(
                    oldBloom.gameObject,
                    templates.transform,
                    "Bubble Template");
                RemoveLegacyInterfaces(bubble);
                templates.SetActive(false);

                TraceManager traceManager = root.AddComponent<TraceManager>();
                SerializedObject traceData = new SerializedObject(traceManager);
                traceData.FindProperty("bubbleTemplate").objectReferenceValue =
                    bubble;
                traceData.FindProperty("visualOrigin").vector3Value =
                    Vector3.zero;
                traceData.FindProperty("visualScale").vector3Value =
                    Vector3.one;
                traceData.FindProperty("destroyEffect").objectReferenceValue =
                    destroyEffect;
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
        foreach (MonoBehaviour behaviour in
                 target.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour != null &&
                behaviour.GetType().Assembly == typeof(BubbleBloom).Assembly)
            {
                Object.DestroyImmediate(behaviour);
            }
        }
    }
}
#endif
