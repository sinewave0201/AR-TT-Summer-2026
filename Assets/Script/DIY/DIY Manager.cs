using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DIYManager : MonoBehaviour
{
    private sealed class ModelSurface
    {
        public Renderer Renderer;
        public Mesh Mesh;
        public MeshCollider Collider;
        public Material Material;
        public RenderTexture PaintTexture;
        public Vector2 LastUv;
        public bool HasLastUv;
    }

    private const string PaintTextureProperty = "_PaintTexture";

    [Header("AR Raycasting")]
    [SerializeField] private Camera arCamera;
    [SerializeField] private LayerMask paintableLayers;

    [Header("Mode")]
    [SerializeField] private bool paintMode = true;
    [SerializeField] private bool eraserMode;

    [Header("Paint Controls")]
    [SerializeField] private FlexibleColorPicker colorPicker;
    [SerializeField] private Slider brushSizeSlider;
    [SerializeField, Min(0.001f)] private float minimumBrushSize = 0.01f;
    [SerializeField, Min(0.001f)] private float maximumBrushSize = 0.1f;

    [Header("Paint Texture")]
    [SerializeField, Min(64)] private int paintTextureResolution = 512;

    [Header("DIY Model Output")]
    [SerializeField] private ChangeBaseModelScript changeBaseModel;
    [SerializeField] private Vector3 outputSize = Vector3.one;

    private readonly Dictionary<Collider, ModelSurface> surfacesByCollider =
        new Dictionary<Collider, ModelSurface>();
    private readonly List<ModelSurface> modelSurfaces =
        new List<ModelSurface>();
    private readonly HashSet<Collider> invalidUvColliders =
        new HashSet<Collider>();
    private readonly Dictionary<Material, RenderTexture> paintTexturesByMaterial =
        new Dictionary<Material, RenderTexture>();

    private ModelSurface activeStrokeSurface;
    private Material brushMaterial;

    private void Awake()
    {
        // DIY uses the AR camera preserved by PersistentARRig. The old
        // serialized DIY camera is inactive after removing its duplicate rig.
        if (arCamera == null || !arCamera.gameObject.activeInHierarchy)
        {
            arCamera = Camera.main;
        }

        Shader brushShader = Shader.Find("Hidden/AR-TT/DIY Paint Brush");

        if (brushShader == null)
        {
            Debug.LogError("The DIY paint brush Shader was not found.", this);
            return;
        }

        brushMaterial = new Material(brushShader);
    }

    private void Update()
    {
        // Only raycast while a finger or mouse button is being held.
        if (!TryGetPointerPosition(out Vector2 screenPosition))
        {
            EndStroke();
            return;
        }

        // Convert the screen position into a hit on a paintable model.
        if (!TryGetPaintableHit(screenPosition, out RaycastHit hit))
        {
            EndStroke();
            return;
        }

        // Paint mode takes priority if both Inspector booleans are enabled.
        if (paintMode)
        {
            PaintAt(hit);
        }
        else if (eraserMode)
        {
            EraseAt(hit);
        }
    }

    // Reads the current touch position on an AR device. Mouse input is
    // included so painting can also be tested inside the Unity Editor.
    private static bool TryGetPointerPosition(out Vector2 screenPosition)
    {
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.isPressed)
        {
            screenPosition =
                Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        screenPosition = default;
        return false;
    }

    // Casts a ray from the AR camera through the supplied screen position.
    // Unlike ARRaycastManager, this Physics raycast detects the virtual
    // model's MeshCollider rather than a real-world tracked AR plane.
    private bool TryGetPaintableHit(
        Vector2 screenPosition,
        out RaycastHit paintHit)
    {
        paintHit = default;

        // The AR camera converts the finger's 2D screen position into a
        // ray travelling through the 3D scene.
        if (arCamera == null)
        {
            Debug.LogError("DIYManager needs an AR Camera reference.", this);
            return false;
        }

        Ray cameraRay = arCamera.ScreenPointToRay(screenPosition);

        return Physics.Raycast(
            cameraRay,
            out paintHit,
            Mathf.Infinity,
            paintableLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    public void PaintAt(RaycastHit hit)
    {
        ApplyBrushAt(hit, false);
    }

    private void EraseAt(RaycastHit hit)
    {
        ApplyBrushAt(hit, true);
    }

    private void ApplyBrushAt(RaycastHit hit, bool erase)
    {
        if (brushMaterial == null || !TryGetSurface(hit, out ModelSurface surface))
        {
            return;
        }

        // Starting on a different model begins a new, disconnected stroke.
        if (activeStrokeSurface != surface)
        {
            EndStroke();
            activeStrokeSurface = surface;
        }

        Color paintColor = colorPicker != null
            ? colorPicker.GetColorFullAlpha()
            : Color.white;

        brushMaterial.SetColor("_BrushColor", paintColor);
        brushMaterial.SetFloat("_Erase", erase ? 1f : 0f);

        PaintStroke(surface, hit.textureCoord);
    }

    private bool TryGetSurface(RaycastHit hit, out ModelSurface surface)
    {
        if (surfacesByCollider.TryGetValue(hit.collider, out surface))
        {
            return true;
        }

        if (hit.collider is not MeshCollider meshCollider)
        {
            WarnInvalidUvCollider(hit.collider);
            return false;
        }

        return TryRegisterSurface(meshCollider, out surface);
    }

    // Connects a newly spawned model to its cached paint textures before
    // the player touches it. This prevents old paint from appearing late.
    public void RegisterModel(GameObject model)
    {
        if (model == null)
        {
            return;
        }

        foreach (MeshCollider meshCollider in
            model.GetComponentsInChildren<MeshCollider>(true))
        {
            TryRegisterSurface(meshCollider, out _);
        }
    }

    private bool TryRegisterSurface(
        MeshCollider meshCollider,
        out ModelSurface surface)
    {
        if (meshCollider == null)
        {
            surface = null;
            return false;
        }

        if (surfacesByCollider.TryGetValue(meshCollider, out surface))
        {
            return true;
        }

        Renderer modelRenderer = meshCollider.GetComponent<Renderer>();
        Mesh modelMesh = GetRendererMesh(modelRenderer);

        if (modelRenderer == null || modelMesh == null)
        {
            WarnInvalidUvCollider(meshCollider);
            return false;
        }

        // The shared material asset is a stable key that remains the same
        // when this prefab is destroyed and spawned again.
        Material sourceMaterial = modelRenderer.sharedMaterial;
        if (sourceMaterial == null)
        {
            Debug.LogError($"{modelRenderer.name} has no material.", modelRenderer);
            surface = null;
            return false;
        }

        // renderer.material creates a runtime material instance. The
        // prefab's source material asset therefore remains unchanged.
        Material materialInstance = modelRenderer.material;
        if (!materialInstance.HasProperty(PaintTextureProperty))
        {
            Debug.LogError(
                $"{modelRenderer.name}'s material needs a {PaintTextureProperty} property.",
                modelRenderer);
            surface = null;
            return false;
        }

        if (!paintTexturesByMaterial.TryGetValue(
            sourceMaterial,
            out RenderTexture paintTexture))
        {
            paintTexture = CreatePaintTexture(sourceMaterial.name);
            paintTexturesByMaterial[sourceMaterial] = paintTexture;
        }

        materialInstance.SetTexture(PaintTextureProperty, paintTexture);

        surface = new ModelSurface
        {
            Renderer = modelRenderer,
            Mesh = modelMesh,
            Collider = meshCollider,
            Material = materialInstance,
            PaintTexture = paintTexture
        };

        surfacesByCollider[meshCollider] = surface;
        modelSurfaces.Add(surface);
        return true;
    }

    private void PaintStroke(ModelSurface surface, Vector2 currentUv)
    {
        if (!surface.HasLastUv)
        {
            PaintTexture(surface.PaintTexture, currentUv, currentUv);
            surface.LastUv = currentUv;
            surface.HasLastUv = true;
            return;
        }

        // Do not draw a long line across disconnected UV islands.
        if (Vector2.Distance(surface.LastUv, currentUv) > 0.25f)
        {
            PaintTexture(surface.PaintTexture, currentUv, currentUv);
        }
        else
        {
            PaintTexture(surface.PaintTexture, surface.LastUv, currentUv);
        }

        surface.LastUv = currentUv;
    }

    private void PaintTexture(
        RenderTexture paintTexture,
        Vector2 startUv,
        Vector2 endUv)
    {
        float sliderPosition = brushSizeSlider != null
            ? brushSizeSlider.normalizedValue
            : 0.5f;
        float brushRadius = Mathf.Lerp(
            minimumBrushSize,
            maximumBrushSize,
            sliderPosition);

        brushMaterial.SetVector("_BrushStart", startUv);
        brushMaterial.SetVector("_BrushEnd", endUv);
        brushMaterial.SetFloat("_BrushRadius", brushRadius);

        RenderTexture temporary = RenderTexture.GetTemporary(
            paintTexture.width,
            paintTexture.height,
            0,
            paintTexture.format,
            RenderTextureReadWrite.Linear);

        Graphics.Blit(paintTexture, temporary, brushMaterial);
        Graphics.Blit(temporary, paintTexture);
        RenderTexture.ReleaseTemporary(temporary);
    }

    private RenderTexture CreatePaintTexture(string rendererName)
    {
        RenderTexture paintTexture = new RenderTexture(
            paintTextureResolution,
            paintTextureResolution,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear)
        {
            name = $"{rendererName}_PaintTexture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        paintTexture.Create();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = paintTexture;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = previous;

        return paintTexture;
    }

    // Clears the painting for every model material cached in this DIY
    // session. Models that are spawned again will therefore start clean.
    public void ResetAllPaint()
    {
        EndStroke();

        foreach (RenderTexture paintTexture in paintTexturesByMaterial.Values)
        {
            ClearPaintTexture(paintTexture);
        }
    }

    private static void ClearPaintTexture(RenderTexture paintTexture)
    {
        if (paintTexture == null)
        {
            return;
        }

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = paintTexture;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = previous;
    }

    // Creates a customized runtime copy of the currently selected prefab.
    // Runtime builds cannot create a new prefab asset, so the result is
    // stored in DIYModelTransfer and kept alive across scene changes.
    public void SaveChangeToDIYMod()
    {
        GameObject selectedPrefab = changeBaseModel != null
            ? changeBaseModel.CurrentModelPrefab
            : null;

        if (selectedPrefab == null)
        {
            Debug.LogError(
                "DIYManager needs a currently selected model prefab.",
                this);
            return;
        }

        GameObject outputModel = Instantiate(selectedPrefab);
        outputModel.name = $"{selectedPrefab.name}_DIYModel";
        outputModel.transform.SetPositionAndRotation(
            Vector3.zero,
            Quaternion.identity);
        outputModel.transform.localScale = outputSize;

        List<Material> outputMaterials = new List<Material>();
        List<Texture2D> outputTextures = new List<Texture2D>();
        Dictionary<Material, Texture2D> copiedPaintTextures =
            new Dictionary<Material, Texture2D>();

        foreach (Renderer outputRenderer in
            outputModel.GetComponentsInChildren<Renderer>(true))
        {
            Material[] sourceMaterials = outputRenderer.sharedMaterials;
            Material[] materialCopies = new Material[sourceMaterials.Length];

            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                Material sourceMaterial = sourceMaterials[i];
                if (sourceMaterial == null)
                {
                    continue;
                }

                Material materialCopy = new Material(sourceMaterial);
                materialCopies[i] = materialCopy;
                outputMaterials.Add(materialCopy);

                if (!paintTexturesByMaterial.TryGetValue(
                    sourceMaterial,
                    out RenderTexture cachedPaint))
                {
                    continue;
                }

                if (!copiedPaintTextures.TryGetValue(
                    sourceMaterial,
                    out Texture2D savedPaint))
                {
                    savedPaint = CopyPaintTexture(cachedPaint);
                    copiedPaintTextures[sourceMaterial] = savedPaint;
                    outputTextures.Add(savedPaint);
                }

                if (materialCopy.HasProperty(PaintTextureProperty))
                {
                    materialCopy.SetTexture(
                        PaintTextureProperty,
                        savedPaint);
                }
            }

            outputRenderer.sharedMaterials = materialCopies;
        }

        DIYModelTransfer.Store(
            outputModel,
            outputMaterials,
            outputTextures);
    }

    private static Texture2D CopyPaintTexture(RenderTexture source)
    {
        Texture2D copy = new Texture2D(
            source.width,
            source.height,
            TextureFormat.RGBA32,
            false,
            true)
        {
            name = $"{source.name}_Saved",
            filterMode = source.filterMode,
            wrapMode = source.wrapMode
        };

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = source;
        copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        copy.Apply(false, false);
        RenderTexture.active = previous;

        return copy;
    }

    private void EndStroke()
    {
        if (activeStrokeSurface != null)
        {
            activeStrokeSurface.HasLastUv = false;
            activeStrokeSurface = null;
        }
    }

    private static Mesh GetRendererMesh(Renderer modelRenderer)
    {
        if (modelRenderer == null)
        {
            return null;
        }

        if (modelRenderer.TryGetComponent(out MeshFilter meshFilter))
        {
            return meshFilter.sharedMesh;
        }

        if (modelRenderer is SkinnedMeshRenderer skinnedRenderer)
        {
            return skinnedRenderer.sharedMesh;
        }

        return null;
    }

    private void WarnInvalidUvCollider(Collider targetCollider)
    {
        if (targetCollider == null || !invalidUvColliders.Add(targetCollider))
        {
            return;
        }

        Debug.LogWarning(
            $"{targetCollider.name} needs a MeshCollider on the same object " +
            "as its UV-mapped Renderer for DIY painting.",
            targetCollider);
    }

    private void OnDestroy()
    {
        foreach (RenderTexture paintTexture in paintTexturesByMaterial.Values)
        {
            if (paintTexture != null)
            {
                paintTexture.Release();
                Destroy(paintTexture);
            }
        }

        foreach (ModelSurface surface in modelSurfaces)
        {
            if (surface.Material != null)
            {
                Destroy(surface.Material);
            }
        }

        if (brushMaterial != null)
        {
            Destroy(brushMaterial);
        }
    }


    //set paint or erase
    public void SetPaint()
    {
        paintMode = true;
        eraserMode = false;
    }

    public void SetErase()
    {
        paintMode = false;
        eraserMode = true;
    }
}
