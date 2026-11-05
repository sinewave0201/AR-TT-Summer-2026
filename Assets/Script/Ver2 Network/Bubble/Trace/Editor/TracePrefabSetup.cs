#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class TracePrefabSetup
{
    internal const string OutputPath = "Assets/Prefab/Network/TraceRobot.prefab";
    private const string OldSettingsPath = "Assets/Prefab/TTsettingPrefab.prefab";
    private const string CurrentRobotPath = "Assets/Prefab/Robot Prefab/robot.prefab";
    private const string SessionKey = "ARTT.TracePrefabSetup.v1";

    static TracePrefabSetup()
    {
        EditorApplication.delayCall += RunOnce;
    }

    private static void RunOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SessionState.GetBool(SessionKey, false) ||
            AssetDatabase.LoadAssetAtPath<GameObject>(OutputPath) != null)
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);
        BuildTraceRobotPrefab();
    }

    [MenuItem("Tools/Thought Trader/Rebuild Trace Robot Prefab")]
    public static void BuildTraceRobotPrefab()
    {
        GameObject oldRoot = PrefabUtility.LoadPrefabContents(OldSettingsPath);
        try
        {
            BubbleBloom oldBloom = oldRoot.GetComponentInChildren<BubbleBloom>(true);
            GameObject currentRobot =
                AssetDatabase.LoadAssetAtPath<GameObject>(CurrentRobotPath);
            if (oldBloom == null || currentRobot == null)
            {
                Debug.LogError("Trace prefab setup could not find the old bloom or current robot.");
                return;
            }

            SerializedObject bloomData = new SerializedObject(oldBloom);
            GameObject potSource = GetObject(bloomData, "pot");
            GameObject flowerSource = GetObject(bloomData, "flower");
            GameObject bubbleSource = oldBloom.gameObject;
            if (potSource == null || flowerSource == null || bubbleSource == null)
            {
                Debug.LogError("TTsettingPrefab is missing BubbleBloom's pot/flower references.");
                return;
            }

            GameObject root = new GameObject("TraceRobot");
            try
            {
                GameObject robot = (GameObject)PrefabUtility.InstantiatePrefab(currentRobot);
                robot.name = "robot";
                robot.transform.SetParent(root.transform, false);
                robot.transform.localPosition = Vector3.zero;
                robot.transform.localRotation = Quaternion.identity;
                RemoveNestedNetworkObject(robot);

                GameObject templates = new GameObject("Trace Templates");
                templates.transform.SetParent(root.transform, false);

                GameObject bubble = DuplicateTemplate(
                    bubbleSource,
                    templates.transform,
                    "Bubble Template");
                RemoveInteractiveBubbleScripts(bubble);
                GameObject pot = DuplicateTemplate(
                    potSource,
                    templates.transform,
                    "Pot Template");
                GameObject flower = DuplicateTemplate(
                    flowerSource,
                    templates.transform,
                    "Flower Template");
                templates.SetActive(false);

                TraceManager traceManager = root.AddComponent<TraceManager>();
                SerializedObject traceData = new SerializedObject(traceManager);
                traceData.FindProperty("bubbleTemplate").objectReferenceValue = bubble;
                traceData.FindProperty("potTemplate").objectReferenceValue = pot;
                traceData.FindProperty("flowerTemplate").objectReferenceValue = flower;
                traceData.FindProperty("animationManager").objectReferenceValue =
                    robot.GetComponentInChildren<AnimationManager>(true);
                traceData.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, OutputPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Trace robot prefab rebuilt at {OutputPath}.");
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
        Vector3 position = source.transform.localPosition;
        Quaternion rotation = source.transform.localRotation;
        Vector3 scale = source.transform.localScale;
        GameObject copy = Object.Instantiate(source);
        copy.name = name;
        copy.transform.SetParent(parent, false);
        copy.transform.localPosition = position;
        copy.transform.localRotation = rotation;
        copy.transform.localScale = scale;
        copy.SetActive(false);
        return copy;
    }

    private static void RemoveNestedNetworkObject(GameObject robot)
    {
        foreach (Component component in robot.GetComponents<Component>())
        {
            if (component != null && component.GetType().Name == "NetworkObject")
            {
                Object.DestroyImmediate(component);
            }
        }
    }

    private static void RemoveInteractiveBubbleScripts(GameObject bubble)
    {
        Component[] components = bubble.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component is BubbleBehaviorManager ||
                component is BubbleBloom ||
                component is BubbleBurn ||
                component is BubbleClean)
            {
                Object.DestroyImmediate(component);
            }
        }
    }
}
#endif
