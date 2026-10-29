using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClientRobotControlUI : MonoBehaviour
{
    [Header("Scroll View Content")]
    [SerializeField] private Transform animationContent;
    [SerializeField] private Transform voiceRoleContent;
    [SerializeField] private Transform soundContent;

    [Header("Button")]
    [SerializeField] private Button controlButtonPrefab;
    [SerializeField, Min(1f)] private float buttonHeight = 50f;

    private readonly List<GameObject> generatedButtons = new List<GameObject>();
    private readonly Dictionary<SoundManager.VoiceRole, TMP_Text> roleTexts =
        new Dictionary<SoundManager.VoiceRole, TMP_Text>();

    private InheritNetworkBehavior bridge;
    private SoundManager.VoiceRole selectedRole = SoundManager.VoiceRole.Bubbly;

    private void Start()
    {
        if (controlButtonPrefab == null || animationContent == null ||
            voiceRoleContent == null || soundContent == null)
        {
            Debug.LogError(
                "ClientRobotControlUI needs three Content transforms and a Button prefab.",
                this);
            return;
        }

        ConfigureContent(animationContent, false);
        ConfigureContent(voiceRoleContent, true);
        ConfigureContent(soundContent, false);

        GenerateAnimationButtons();
        GenerateVoiceRoleButtons();
        GenerateSoundButtons();
        UpdateSelectedRoleVisual();
    }

    private void GenerateAnimationButtons()
    {
        foreach (AnimationManager.RobotAnimation animation in
                 Enum.GetValues(typeof(AnimationManager.RobotAnimation)))
        {
            AnimationManager.RobotAnimation selectedAnimation = animation;
            CreateButton(
                animationContent,
                animation.ToString(),
                () => SendAnimation(selectedAnimation));
        }
    }

    private void GenerateVoiceRoleButtons()
    {
        foreach (SoundManager.VoiceRole role in
                 Enum.GetValues(typeof(SoundManager.VoiceRole)))
        {
            SoundManager.VoiceRole selectedVoiceRole = role;
            Button button = CreateButton(
                voiceRoleContent,
                role.ToString(),
                () => SelectVoiceRole(selectedVoiceRole));

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                roleTexts[role] = label;
            }
        }
    }

    private void GenerateSoundButtons()
    {
        foreach (SoundManager.RobotSound sound in
                 Enum.GetValues(typeof(SoundManager.RobotSound)))
        {
            SoundManager.RobotSound selectedSound = sound;
            CreateButton(
                soundContent,
                sound.ToString(),
                () => SendSound(selectedSound));
        }
    }

    private void SelectVoiceRole(SoundManager.VoiceRole role)
    {
        selectedRole = role;
        UpdateSelectedRoleVisual();

        InheritNetworkBehavior currentBridge = GetBridge();
        if (currentBridge != null)
        {
            currentBridge.RequestVoiceRole(role);
        }
    }

    private void SendAnimation(AnimationManager.RobotAnimation animation)
    {
        InheritNetworkBehavior currentBridge = GetBridge();
        if (currentBridge != null)
        {
            currentBridge.RequestAnimation(animation);
        }
    }

    private void SendSound(SoundManager.RobotSound sound)
    {
        InheritNetworkBehavior currentBridge = GetBridge();
        if (currentBridge != null)
        {
            currentBridge.RequestVoiceRole(selectedRole);
            currentBridge.RequestSound(sound);
        }
    }

    private InheritNetworkBehavior GetBridge()
    {
        if (bridge == null)
        {
            bridge = FindFirstObjectByType<InheritNetworkBehavior>();
        }

        if (bridge == null || !bridge.IsSpawned)
        {
            Debug.LogWarning(
                "RobotControlBridge is not available. Connect to the Android Host first.",
                this);
            return null;
        }

        return bridge;
    }

    private Button CreateButton(
        Transform parent,
        string labelText,
        UnityEngine.Events.UnityAction onClick)
    {
        Button button = Instantiate(controlButtonPrefab, parent);
        button.name = $"{labelText} Button";
        button.transform.localScale = Vector3.one;
        button.onClick.AddListener(onClick);

        if (button.transform is RectTransform rect)
        {
            rect.anchoredPosition = Vector2.zero;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, buttonHeight);
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = labelText;
        }

        generatedButtons.Add(button.gameObject);
        return button;
    }

    private void ConfigureContent(Transform content, bool horizontal)
    {
        if (horizontal)
        {
            HorizontalLayoutGroup group = content.GetComponent<HorizontalLayoutGroup>();
            if (group == null)
            {
                group = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            group.spacing = 8f;
            group.childControlWidth = false;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
        }
        else
        {
            VerticalLayoutGroup group = content.GetComponent<VerticalLayoutGroup>();
            if (group == null)
            {
                group = content.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            group.spacing = 5f;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
        }

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = horizontal
            ? ContentSizeFitter.FitMode.PreferredSize
            : ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = horizontal
            ? ContentSizeFitter.FitMode.Unconstrained
            : ContentSizeFitter.FitMode.PreferredSize;
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
        for (int i = 0; i < generatedButtons.Count; i++)
        {
            if (generatedButtons[i] != null)
            {
                Destroy(generatedButtons[i]);
            }
        }
    }
}
