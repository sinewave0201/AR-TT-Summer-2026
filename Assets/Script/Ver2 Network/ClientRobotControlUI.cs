using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClientRobotControlUI : MonoBehaviour
{
    [Serializable]
    private class TraceHistory
    {
        public List<TraceHistoryItem> traces = new List<TraceHistoryItem>();
    }

    [Serializable]
    private class TraceHistoryItem
    {
        public string id;
        public string createdAt;
        public string content;
    }

    [Header("Legacy content references (rebuilt at runtime)")]
    [SerializeField] private Transform animationContent;
    [SerializeField] private Transform voiceRoleContent;
    [SerializeField] private Transform soundContent;

    [Header("Button")]
    [SerializeField] private Button controlButtonPrefab;
    [SerializeField, Min(1f)] private float buttonHeight = 50f;

    private readonly List<GameObject> generatedObjects = new List<GameObject>();
    private readonly Dictionary<SoundManager.VoiceRole, TMP_Text> roleTexts =
        new Dictionary<SoundManager.VoiceRole, TMP_Text>();
    private readonly HashSet<string> displayedTraceIds = new HashSet<string>();

    private InheritNetworkBehavior bridge;
    private SoundManager.VoiceRole selectedRole = SoundManager.VoiceRole.Bubbly;
    private Transform traceContent;
    private Button newTraceButton;

    private void Start()
    {
        if (controlButtonPrefab == null)
        {
            Debug.LogError("ClientRobotControlUI needs a Button prefab.", this);
            return;
        }

        ConfigureFullscreenCanvas();
        HideLegacyUI();
        BuildTwoColumnLayout();
        GenerateAnimationButtons();
        GenerateVoiceRoleButtons();
        GenerateSoundButtons();
        UpdateSelectedRoleVisual();

        InheritNetworkBehavior.TraceHistoryReceived += DisplayHistory;
        StartCoroutine(RequestHistoryWhenConnected());
    }

    private void BuildTwoColumnLayout()
    {
        GameObject workspace = CreateUIObject("Robot Control Workspace", transform);
        RectTransform workspaceRect = workspace.GetComponent<RectTransform>();
        workspaceRect.anchorMin = Vector2.zero;
        workspaceRect.anchorMax = Vector2.one;
        workspaceRect.offsetMin = Vector2.zero;
        workspaceRect.offsetMax = Vector2.zero;
        AddImage(workspace, new Color32(15, 18, 28, 255));

        VerticalLayoutGroup page = workspace.AddComponent<VerticalLayoutGroup>();
        page.padding = new RectOffset(24, 24, 20, 24);
        page.spacing = 16f;
        page.childControlWidth = true;
        page.childControlHeight = true;
        page.childForceExpandWidth = true;
        page.childForceExpandHeight = false;

        CreateConnectionBar(workspace.transform);

        GameObject contentArea = CreateUIObject("Main Content", workspace.transform);
        contentArea.AddComponent<LayoutElement>().flexibleHeight = 1f;
        HorizontalLayoutGroup columns = contentArea.AddComponent<HorizontalLayoutGroup>();
        columns.padding = new RectOffset(0, 0, 0, 0);
        columns.spacing = 14f;
        columns.childControlWidth = true;
        columns.childControlHeight = true;
        columns.childForceExpandWidth = true;
        columns.childForceExpandHeight = true;

        GameObject left = CreateUIObject("Robot Controls", contentArea.transform);
        left.AddComponent<LayoutElement>().flexibleWidth = 0.8f;
        VerticalLayoutGroup leftColumn = left.AddComponent<VerticalLayoutGroup>();
        leftColumn.spacing = 10f;
        leftColumn.childControlWidth = true;
        leftColumn.childControlHeight = true;
        leftColumn.childForceExpandWidth = true;
        leftColumn.childForceExpandHeight = true;

        animationContent = CreateSection(left.transform, "ANIMATION", 1f);
        CreateSoundSection(left.transform);

        GameObject right = CreateUIObject("Trace Panel", contentArea.transform);
        right.AddComponent<LayoutElement>().flexibleWidth = 1.45f;
        AddImage(right, new Color32(27, 31, 45, 255));
        VerticalLayoutGroup rightColumn = right.AddComponent<VerticalLayoutGroup>();
        rightColumn.padding = new RectOffset(14, 14, 12, 12);
        rightColumn.spacing = 10f;
        rightColumn.childControlWidth = true;
        rightColumn.childControlHeight = true;
        rightColumn.childForceExpandWidth = true;
        rightColumn.childForceExpandHeight = false;

        CreateLabel(right.transform, "TRACE", 30f, FontStyles.Bold, 42f);
        newTraceButton = CreateControlButton(
            right.transform,
            "CREATE NEW TRACE",
            CreateNewTrace);
        LayoutElement newTraceLayout = GetOrAddLayout(newTraceButton.gameObject);
        newTraceLayout.preferredHeight = 54f;
        newTraceLayout.flexibleHeight = 0f;

        traceContent = CreateScrollContent(right.transform, "Trace List", true);
        traceContent.parent.parent.GetComponent<LayoutElement>().flexibleHeight = 1f;
    }

    private void CreateConnectionBar(Transform parent)
    {
        GameObject bar = CreateUIObject("Connection Bar", parent);
        AddImage(bar, new Color32(27, 31, 45, 255));
        LayoutElement barLayout = bar.AddComponent<LayoutElement>();
        barLayout.preferredHeight = 104f;
        barLayout.flexibleHeight = 0f;

        HorizontalLayoutGroup layout = bar.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 12, 12);
        layout.spacing = 24f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        GameObject ipGroup = CreateUIObject("Host IP Group", bar.transform);
        LayoutElement ipLayout = ipGroup.AddComponent<LayoutElement>();
        ipLayout.flexibleWidth = 1f;
        ipLayout.preferredWidth = 420f;
        VerticalLayoutGroup ipColumn = ipGroup.AddComponent<VerticalLayoutGroup>();
        ipColumn.spacing = 5f;
        ipColumn.childControlWidth = true;
        ipColumn.childControlHeight = true;
        ipColumn.childForceExpandWidth = true;
        ipColumn.childForceExpandHeight = false;
        CreateLabel(ipGroup.transform, "HOST IP INPUT", 22f, FontStyles.Bold, 30f);
        TMP_InputField hostInput = CreateInputField(ipGroup.transform, "Enter Android Host IP...");
        hostInput.lineType = TMP_InputField.LineType.SingleLine;
        hostInput.gameObject.GetComponent<LayoutElement>().preferredHeight = 42f;

        GameObject statusGroup = CreateUIObject("Connection Status Group", bar.transform);
        LayoutElement statusLayout = statusGroup.AddComponent<LayoutElement>();
        statusLayout.flexibleWidth = 1f;
        statusLayout.preferredWidth = 420f;
        VerticalLayoutGroup statusColumn = statusGroup.AddComponent<VerticalLayoutGroup>();
        statusColumn.spacing = 5f;
        statusColumn.childControlWidth = true;
        statusColumn.childControlHeight = true;
        statusColumn.childForceExpandWidth = true;
        statusColumn.childForceExpandHeight = false;
        CreateLabel(statusGroup.transform, "CONNECTION STATUS", 22f, FontStyles.Bold, 30f);
        TMP_Text statusText = CreateLabel(
            statusGroup.transform,
            "Disconnected",
            18f,
            FontStyles.Normal,
            42f);
        statusText.color = new Color32(183, 190, 208, 255);

        Button connect = CreateControlButton(bar.transform, "CONNECT", null);
        LayoutElement connectLayout = GetOrAddLayout(connect.gameObject);
        connectLayout.preferredWidth = 260f;
        connectLayout.preferredHeight = 50f;

        LanClientConnector connector = GetComponent<LanClientConnector>();
        if (connector != null)
        {
            connector.BindUI(hostInput, connect, statusText);
        }
    }

    private Transform CreateSection(Transform parent, string title, float flexibleHeight)
    {
        GameObject section = CreateUIObject($"{title} Section", parent);
        AddImage(section, new Color32(27, 31, 45, 255));
        section.AddComponent<LayoutElement>().flexibleHeight = flexibleHeight;

        VerticalLayoutGroup layout = section.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 10);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateLabel(section.transform, title, 23f, FontStyles.Bold, 32f);
        Transform content = CreateScrollContent(section.transform, $"{title} List", false);
        content.parent.parent.GetComponent<LayoutElement>().flexibleHeight = 1f;
        return content;
    }

    private void CreateSoundSection(Transform parent)
    {
        GameObject section = CreateUIObject("SOUND Section", parent);
        AddImage(section, new Color32(27, 31, 45, 255));
        section.AddComponent<LayoutElement>().flexibleHeight = 1.15f;

        VerticalLayoutGroup layout = section.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 10);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateLabel(section.transform, "SOUND", 23f, FontStyles.Bold, 32f);
        CreateLabel(section.transform, "Voice", 15f, FontStyles.Normal, 22f);

        GameObject roles = CreateUIObject("Voice Roles", section.transform);
        roles.AddComponent<LayoutElement>().preferredHeight = buttonHeight;
        HorizontalLayoutGroup roleGroup = roles.AddComponent<HorizontalLayoutGroup>();
        roleGroup.spacing = 6f;
        roleGroup.childControlWidth = true;
        roleGroup.childControlHeight = true;
        roleGroup.childForceExpandWidth = true;
        roleGroup.childForceExpandHeight = true;
        voiceRoleContent = roles.transform;

        CreateLabel(section.transform, "Sounds", 15f, FontStyles.Normal, 22f);
        soundContent = CreateScrollContent(section.transform, "Sound List", false);
        soundContent.parent.parent.GetComponent<LayoutElement>().flexibleHeight = 1f;
    }

    private Transform CreateScrollContent(Transform parent, string name, bool roomyRows)
    {
        GameObject scrollObject = CreateUIObject(name, parent);
        LayoutElement scrollLayout = scrollObject.AddComponent<LayoutElement>();
        scrollLayout.minHeight = 60f;
        scrollLayout.flexibleHeight = 1f;
        AddImage(scrollObject, new Color32(12, 15, 24, 160));
        ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        GameObject viewport = CreateUIObject("Viewport", scrollObject.transform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect, 4f);
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = viewportRect;

        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup group = content.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(4, 4, 4, 4);
        group.spacing = roomyRows ? 8f : 5f;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRect;
        return content.transform;
    }

    private void GenerateAnimationButtons()
    {
        foreach (AnimationManager.RobotAnimation animation in
                 Enum.GetValues(typeof(AnimationManager.RobotAnimation)))
        {
            AnimationManager.RobotAnimation choice = animation;
            CreateControlButton(animationContent, animation.ToString(), () => SendAnimation(choice));
        }
    }

    private void GenerateVoiceRoleButtons()
    {
        foreach (SoundManager.VoiceRole role in Enum.GetValues(typeof(SoundManager.VoiceRole)))
        {
            SoundManager.VoiceRole choice = role;
            Button button = CreateControlButton(
                voiceRoleContent,
                role.ToString(),
                () => SelectVoiceRole(choice));
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontSize = 14f;
                roleTexts[role] = label;
            }
        }
    }

    private void GenerateSoundButtons()
    {
        foreach (SoundManager.RobotSound sound in Enum.GetValues(typeof(SoundManager.RobotSound)))
        {
            SoundManager.RobotSound choice = sound;
            CreateControlButton(soundContent, sound.ToString(), () => SendSound(choice));
        }
    }

    private void CreateNewTrace()
    {
        InheritNetworkBehavior currentBridge = GetBridge();
        if (currentBridge == null)
        {
            return;
        }

        string traceId = Guid.NewGuid().ToString("N");
        currentBridge.RequestNewTrace(traceId);
        AddEditableTraceRow(traceId, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        newTraceButton.interactable = false;
    }

    private void AddEditableTraceRow(string traceId, string createdAt)
    {
        if (!displayedTraceIds.Add(traceId))
        {
            return;
        }

        GameObject row = CreateTraceRow(createdAt);
        TMP_InputField input = CreateInputField(row.transform, "What is this trace?");
        Button create = CreateControlButton(row.transform, "CREATE", null);
        GetOrAddLayout(create.gameObject).preferredWidth = 92f;
        create.onClick.AddListener(() =>
        {
            InheritNetworkBehavior currentBridge = GetBridge();
            if (currentBridge == null)
            {
                return;
            }

            currentBridge.RequestCompleteTrace(traceId, input.text);
            input.interactable = false;
            create.interactable = false;
            TMP_Text label = create.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = "GROWING";
            }
            StartCoroutine(UnlockNewTraceAfterGrowth(label));
        });
    }

    private GameObject CreateTraceRow(string createdAt)
    {
        GameObject row = CreateUIObject("Trace Row", traceContent);
        AddImage(row, new Color32(39, 45, 63, 255));
        row.AddComponent<LayoutElement>().preferredHeight = 64f;
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 7, 7);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        TMP_Text time = CreateLabel(row.transform, createdAt, 13f, FontStyles.Normal, 50f);
        time.gameObject.GetComponent<LayoutElement>().preferredWidth = 120f;
        return row;
    }

    private TMP_InputField CreateInputField(Transform parent, string placeholderText)
    {
        GameObject root = CreateUIObject("Trace Input", parent);
        AddImage(root, new Color32(245, 246, 250, 255));
        root.AddComponent<LayoutElement>().flexibleWidth = 1f;
        TMP_InputField input = root.AddComponent<TMP_InputField>();

        GameObject area = CreateUIObject("Text Area", root.transform);
        RectTransform areaRect = area.GetComponent<RectTransform>();
        Stretch(areaRect, 8f);
        area.AddComponent<RectMask2D>();

        TMP_Text placeholder = CreateRawText(
            area.transform,
            placeholderText,
            15f,
            new Color32(100, 105, 118, 180));
        TMP_Text text = CreateRawText(
            area.transform,
            string.Empty,
            15f,
            new Color32(25, 28, 36, 255));
        Stretch(placeholder.rectTransform, 0f);
        Stretch(text.rectTransform, 0f);
        input.textViewport = areaRect;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.MultiLineNewline;
        return input;
    }

    private IEnumerator UnlockNewTraceAfterGrowth(TMP_Text buttonLabel)
    {
        yield return new WaitForSeconds(6.5f);
        if (buttonLabel != null)
        {
            buttonLabel.text = "CREATED";
        }
        if (newTraceButton != null)
        {
            newTraceButton.interactable = true;
        }
    }

    private IEnumerator RequestHistoryWhenConnected()
    {
        while (GetBridge(false) == null)
        {
            yield return new WaitForSeconds(0.5f);
        }
        bridge.RequestTraceHistory();
    }

    private void DisplayHistory(string json)
    {
        TraceHistory history = JsonUtility.FromJson<TraceHistory>(json);
        if (history?.traces == null)
        {
            return;
        }

        foreach (TraceHistoryItem item in history.traces)
        {
            if (!displayedTraceIds.Add(item.id))
            {
                continue;
            }

            GameObject row = CreateTraceRow(item.createdAt);
            TMP_Text content = CreateLabel(row.transform, item.content, 15f, FontStyles.Normal, 50f);
            content.alignment = TextAlignmentOptions.MidlineLeft;
            content.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1f;
            TMP_Text status = CreateLabel(row.transform, "CREATED", 13f, FontStyles.Bold, 50f);
            status.color = new Color32(137, 224, 164, 255);
            status.gameObject.GetComponent<LayoutElement>().preferredWidth = 92f;
        }
    }

    private void SelectVoiceRole(SoundManager.VoiceRole role)
    {
        selectedRole = role;
        UpdateSelectedRoleVisual();
        GetBridge()?.RequestVoiceRole(role);
    }

    private void SendAnimation(AnimationManager.RobotAnimation animation)
    {
        GetBridge()?.RequestAnimation(animation);
    }

    private void SendSound(SoundManager.RobotSound sound)
    {
        InheritNetworkBehavior currentBridge = GetBridge();
        if (currentBridge == null)
        {
            return;
        }
        currentBridge.RequestVoiceRole(selectedRole);
        currentBridge.RequestSound(sound);
    }

    private InheritNetworkBehavior GetBridge(bool warn = true)
    {
        if (bridge == null)
        {
            bridge = FindFirstObjectByType<InheritNetworkBehavior>();
        }
        if (bridge == null || !bridge.IsSpawned)
        {
            if (warn)
            {
                Debug.LogWarning(
                    "RobotControlBridge is unavailable. Connect to the Android Host first.",
                    this);
            }
            return null;
        }
        return bridge;
    }

    private Button CreateControlButton(
        Transform parent,
        string labelText,
        UnityEngine.Events.UnityAction onClick)
    {
        Button button = Instantiate(controlButtonPrefab, parent);
        button.name = $"{labelText} Button";
        button.transform.localScale = Vector3.one;
        button.onClick.RemoveAllListeners();
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }
        LayoutElement layout = GetOrAddLayout(button.gameObject);
        layout.preferredHeight = buttonHeight;
        layout.flexibleHeight = 0f;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = labelText;
        }
        generatedObjects.Add(button.gameObject);
        return button;
    }

    private TMP_Text CreateLabel(
        Transform parent,
        string text,
        float fontSize,
        FontStyles style,
        float height)
    {
        TMP_Text label = CreateRawText(parent, text, fontSize, Color.white);
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        return label;
    }

    private TMP_Text CreateRawText(Transform parent, string text, float fontSize, Color color)
    {
        GameObject textObject = CreateUIObject("Text", parent);
        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.enableWordWrapping = true;
        return label;
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject result = new GameObject(name, typeof(RectTransform));
        result.transform.SetParent(parent, false);
        generatedObjects.Add(result);
        return result;
    }

    private static void AddImage(GameObject target, Color color)
    {
        target.AddComponent<Image>().color = color;
    }

    private static LayoutElement GetOrAddLayout(GameObject target)
    {
        LayoutElement layout = target.GetComponent<LayoutElement>();
        return layout != null ? layout : target.AddComponent<LayoutElement>();
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private void ConfigureFullscreenCanvas()
    {
        transform.localScale = Vector3.one;
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    private void HideLegacyUI()
    {
        List<GameObject> legacyChildren = new List<GameObject>();
        for (int index = 0; index < transform.childCount; index++)
        {
            legacyChildren.Add(transform.GetChild(index).gameObject);
        }

        foreach (GameObject child in legacyChildren)
        {
            child.SetActive(false);
        }
    }

    private void UpdateSelectedRoleVisual()
    {
        foreach (KeyValuePair<SoundManager.VoiceRole, TMP_Text> item in roleTexts)
        {
            if (item.Value != null)
            {
                item.Value.color = item.Key == selectedRole
                    ? new Color32(0xD0, 0x2B, 0xFD, 0xFF)
                    : Color.white;
            }
        }
    }

    private void OnDestroy()
    {
        InheritNetworkBehavior.TraceHistoryReceived -= DisplayHistory;
    }
}
