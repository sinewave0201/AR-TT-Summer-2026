using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ConvertColorPickerPrefab
{
    private const string PrefabPath =
        "Assets/Script/DIY/FlexibleColorPicker/FlexibleColorPicker.prefab";

    static ConvertColorPickerPrefab()
    {
        EditorApplication.delayCall += RunIfNeeded;
    }

    private static void RunIfNeeded()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab != null && prefab.GetComponentInChildren<Text>(true) != null)
        {
            Run();
        }
    }

    public static void Run()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);

        try
        {
            FlexibleColorPicker picker = root.GetComponent<FlexibleColorPicker>();
            InputField oldInput = root.GetComponentInChildren<InputField>(true);
            Dropdown oldDropdown = root.GetComponentInChildren<Dropdown>(true);
            Text[] oldTexts = root.GetComponentsInChildren<Text>(true);
            var convertedTexts = new Dictionary<Text, TextMeshProUGUI>();

            foreach (Text oldText in oldTexts)
            {
                GameObject textObject = oldText.gameObject;
                string value = oldText.text;
                Color color = oldText.color;
                float fontSize = oldText.fontSize;
                FontStyle fontStyle = oldText.fontStyle;
                TextAnchor alignment = oldText.alignment;
                bool raycastTarget = oldText.raycastTarget;
                bool enabled = oldText.enabled;

                Object.DestroyImmediate(oldText);
                TextMeshProUGUI tmpText = textObject.AddComponent<TextMeshProUGUI>();
                tmpText.text = value;
                tmpText.color = color;
                tmpText.fontSize = fontSize;
                tmpText.fontStyle = ConvertFontStyle(fontStyle);
                tmpText.alignment = ConvertAlignment(alignment);
                tmpText.raycastTarget = raycastTarget;
                tmpText.enabled = enabled;
                convertedTexts[oldText] = tmpText;
            }

            TMP_InputField newInput = ConvertInput(oldInput, convertedTexts, picker);
            TMP_Dropdown newDropdown = ConvertDropdown(oldDropdown, convertedTexts, picker);

            SerializedObject pickerData = new SerializedObject(picker);
            pickerData.FindProperty("hexInput").objectReferenceValue = newInput;
            pickerData.FindProperty("modeDropdown").objectReferenceValue = newDropdown;
            pickerData.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Converted {oldTexts.Length} color-picker Text components to TMP.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static TMP_InputField ConvertInput(
        InputField oldInput,
        Dictionary<Text, TextMeshProUGUI> texts,
        FlexibleColorPicker picker)
    {
        TextMeshProUGUI text = texts[oldInput.textComponent];
        TextMeshProUGUI placeholder = texts[oldInput.placeholder as Text];
        GameObject inputObject = oldInput.gameObject;
        Graphic targetGraphic = oldInput.targetGraphic;
        int characterLimit = oldInput.characterLimit;
        string value = oldInput.text;
        bool interactable = oldInput.interactable;

        Object.DestroyImmediate(oldInput);
        TMP_InputField input = inputObject.AddComponent<TMP_InputField>();
        input.targetGraphic = targetGraphic;
        input.textViewport = text.rectTransform.parent as RectTransform;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.characterLimit = characterLimit;
        input.interactable = interactable;
        input.SetTextWithoutNotify(value);
        UnityEventTools.AddPersistentListener(input.onValueChanged, picker.TypeHex);
        UnityEventTools.AddPersistentListener(input.onEndEdit, picker.FinishTypeHex);
        return input;
    }

    private static TMP_Dropdown ConvertDropdown(
        Dropdown oldDropdown,
        Dictionary<Text, TextMeshProUGUI> texts,
        FlexibleColorPicker picker)
    {
        TextMeshProUGUI caption = texts[oldDropdown.captionText];
        TextMeshProUGUI item = texts[oldDropdown.itemText];
        GameObject dropdownObject = oldDropdown.gameObject;
        RectTransform template = oldDropdown.template;
        Graphic targetGraphic = oldDropdown.targetGraphic;
        int value = oldDropdown.value;
        bool interactable = oldDropdown.interactable;
        var options = new List<TMP_Dropdown.OptionData>();

        foreach (Dropdown.OptionData option in oldDropdown.options)
        {
            options.Add(new TMP_Dropdown.OptionData(
                option.text,
                option.image,
                Color.white));
        }

        Object.DestroyImmediate(oldDropdown);
        TMP_Dropdown dropdown = dropdownObject.AddComponent<TMP_Dropdown>();
        dropdown.targetGraphic = targetGraphic;
        dropdown.template = template;
        dropdown.captionText = caption;
        dropdown.itemText = item;
        dropdown.options = options;
        dropdown.interactable = interactable;
        dropdown.SetValueWithoutNotify(value);
        UnityEventTools.AddPersistentListener(dropdown.onValueChanged, picker.ChangeMode);
        return dropdown;
    }

    private static FontStyles ConvertFontStyle(FontStyle style)
    {
        return style switch
        {
            FontStyle.Bold => FontStyles.Bold,
            FontStyle.Italic => FontStyles.Italic,
            FontStyle.BoldAndItalic => FontStyles.Bold | FontStyles.Italic,
            _ => FontStyles.Normal
        };
    }

    private static TextAlignmentOptions ConvertAlignment(TextAnchor alignment)
    {
        return alignment switch
        {
            TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
            TextAnchor.UpperCenter => TextAlignmentOptions.Top,
            TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
            TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
            TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
            TextAnchor.MiddleRight => TextAlignmentOptions.Right,
            TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
            TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
            TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
            _ => TextAlignmentOptions.Center
        };
    }
}
