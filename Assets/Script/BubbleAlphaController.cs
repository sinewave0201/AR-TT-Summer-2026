using UnityEngine;

[ExecuteAlways]
public class BubbleAlphaController : MonoBehaviour
{
    public Renderer bubbleRenderer;

    [Range(0f, 1f)]
    public float alpha = 0f;

    private Material mat;

    void OnEnable()
    {
        InitMaterial();
        ApplyAlpha();
    }

    void Update()
    {
        ApplyAlpha();
    }

    void InitMaterial()
    {
        if (bubbleRenderer == null) return;

        if (Application.isPlaying)
        {
            mat = bubbleRenderer.material;
        }
        else
        {
            mat = bubbleRenderer.sharedMaterial;
        }
    }

    void ApplyAlpha()
    {
        if (bubbleRenderer == null) return;
        if (mat == null) InitMaterial();

        if (mat != null)
        {
            mat.SetFloat("_Alpha", alpha);
        }
    }
}