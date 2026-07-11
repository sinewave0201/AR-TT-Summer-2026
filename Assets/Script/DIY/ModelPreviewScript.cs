using UnityEngine;
using UnityEngine.UI;

public class ModelPreviewScript : MonoBehaviour
{
    [Header("Model")]
    public GameObject prefab;

    [Header("Controls")]
    [SerializeField] private Slider sizeSlider;
    [SerializeField] private Slider rotationSlider;

    private const float MaximumSize = 2f;

    private Vector3 initialScale;
    private Vector3 maximumScale;
    private Vector3 initialEulerAngles;

    private void Awake()
    {
        InitializePrefabValues();

        if (sizeSlider != null)
        {
            ConfigureSlider(sizeSlider);
            sizeSlider.onValueChanged.AddListener(SetSize);
        }

        if (rotationSlider != null)
        {
            ConfigureSlider(rotationSlider);
            rotationSlider.onValueChanged.AddListener(SetRotation);
        }
    }

    private void OnDestroy()
    {
        if (sizeSlider != null)
        {
            sizeSlider.onValueChanged.RemoveListener(SetSize);
        }

        if (rotationSlider != null)
        {
            rotationSlider.onValueChanged.RemoveListener(SetRotation);
        }
    }

    public void SetPrefab(GameObject newPrefab)
    {
        prefab = newPrefab;
        InitializePrefabValues();

        sizeSlider?.SetValueWithoutNotify(0.5f);
        rotationSlider?.SetValueWithoutNotify(0.5f);
    }

    private static void ConfigureSlider(Slider slider)
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.SetValueWithoutNotify(0.5f);
    }

    private void InitializePrefabValues()
    {
        if (prefab == null)
        {
            return;
        }

        initialScale = prefab.transform.localScale;
        initialEulerAngles = prefab.transform.localEulerAngles;

        float largestInitialAxis = Mathf.Max(
            Mathf.Abs(initialScale.x),
            Mathf.Abs(initialScale.y),
            Mathf.Abs(initialScale.z));

        if (largestInitialAxis > MaximumSize)
        {
            initialScale *= MaximumSize / largestInitialAxis;
            prefab.transform.localScale = initialScale;
            largestInitialAxis = MaximumSize;
        }

        maximumScale = largestInitialAxis > Mathf.Epsilon
            ? initialScale * (MaximumSize / largestInitialAxis)
            : Vector3.one * MaximumSize;
    }

    private void SetSize(float sliderValue)
    {
        if (prefab == null)
        {
            return;
        }

        prefab.transform.localScale = sliderValue <= 0.5f
            ? Vector3.Lerp(Vector3.zero, initialScale, sliderValue * 2f)
            : Vector3.Lerp(initialScale, maximumScale, (sliderValue - 0.5f) * 2f);
    }

    private void SetRotation(float sliderValue)
    {
        if (prefab == null)
        {
            return;
        }

        float yOffset = Mathf.Lerp(-180f, 180f, sliderValue);
        prefab.transform.localEulerAngles = new Vector3(
            initialEulerAngles.x,
            initialEulerAngles.y + yOffset,
            initialEulerAngles.z);
    }
}
