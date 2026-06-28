using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BubbleClean : MonoBehaviour
{
    private sealed class CoatingSurface
    {
        public Renderer Renderer;
        public Mesh Mesh;
        public MeshCollider Collider;
        public Material Material;
        public RenderTexture Mask;
        public Vector2 LastUv;
        public bool HasLastUv;
    }
    [Header("Bubble effect & broom")]
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private Renderer ThoughtBubble;
    [SerializeField] private TMP_Text thoughtBubbleText;
    [SerializeField] private AudioSource bubbleSound;
    [SerializeField] private GameObject Broom;

    [Header("Coating Logic")]
    [SerializeField] private GameObject coatingRoot;
    [SerializeField] private Material coatingMaterial;
    [SerializeField, Min(64)] private int maskResolution = 256;
    [SerializeField, Range(0.005f, 0.25f)]
    private float maskBrushRadius = 0.04f;



    [Header("Broom")]
    public bool BroomEnabled = false;

    private readonly List<CoatingSurface> coatingSurfaces = new();
    private readonly Dictionary<Collider, CoatingSurface> surfacesByCollider =
        new();
    private readonly HashSet<Collider> invalidUvColliders = new();
    private CoatingSurface activeStrokeSurface;
    private Material brushMaterial;
    private Vector3 broomOriginalPosition;
    private Quaternion broomOriginalRotation;

    private void Awake()
    {
        if (thoughtBubbleText == null &&
            TryGetComponent(out BubbleBehaviorManager behaviorManager))
        {
            thoughtBubbleText = behaviorManager.bubbleText;
        }

        if (thoughtBubbleText == null)
        {
            Debug.LogWarning(
                "BubbleClean has no Thought Bubble Text reference. " +
                "Assign the bubble TMP_Text in the prefab Inspector.",
                this
            );
        }
    }

    private void Start()
    {
        broomOriginalPosition = Broom.transform.position;
        broomOriginalRotation = Broom.transform.rotation;

        InitializeCoatingSurfaces();
        ResetCleanMasks();
        coatingRoot.SetActive(false);
    }

    public void StartClean()
    {
        StartCoroutine(CleanRoutine());
    }

    private IEnumerator CleanRoutine()
    {
        ParticleSystem[] particles =
            impactEffect.GetComponentsInChildren<ParticleSystem>();
        
        //play the particle effect
        foreach (ParticleSystem particle in particles)
        {
            ParticleSystem.MainModule main = particle.main;
            main.loop = false;
            particle.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
            particle.Play();
        }

        //play the popping sound
        bubbleSound.Play();

        yield return new WaitForSeconds(0.05f);


        ResetCleanMasks();
        coatingRoot.SetActive(true);
        BroomEnabled = true;
        if (thoughtBubbleText != null)
        {
            thoughtBubbleText.text = string.Empty;
        }
        ThoughtBubble.enabled = false;
    }

    public void ResetBubbleClean()
    {
        BroomEnabled = false;
        ResetBroom();

        coatingRoot.SetActive(false);
        ThoughtBubble.enabled = true;
        ResetCleanMasks();
    }

    public void ResetBroom()
    {
        Broom.transform.SetPositionAndRotation(
            broomOriginalPosition,
            broomOriginalRotation
        );

        if (Broom.TryGetComponent(out Rigidbody broomRigidbody))
        {
            broomRigidbody.linearVelocity = Vector3.zero;
            broomRigidbody.angularVelocity = Vector3.zero;
        }
    }

    public void CleanAt(RaycastHit hit)
    {
        if (!BroomEnabled || brushMaterial == null)
        {
            return;
        }

        if (!surfacesByCollider.TryGetValue(
            hit.collider,
            out CoatingSurface surface))
        {
            WarnInvalidUvCollider(hit.collider);
            return;
        }

        if (activeStrokeSurface != surface)
        {
            if (activeStrokeSurface != null)
            {
                activeStrokeSurface.HasLastUv = false;
            }

            activeStrokeSurface = surface;
            surface.HasLastUv = false;
        }

        PaintStroke(surface, hit.textureCoord);
    }

    public bool CanClean(Collider targetCollider)
    {
        return targetCollider != null &&
            surfacesByCollider.ContainsKey(targetCollider);
    }

    public void EndCleanStroke()
    {
        if (activeStrokeSurface != null)
        {
            activeStrokeSurface.HasLastUv = false;
            activeStrokeSurface = null;
        }
    }

    private void InitializeCoatingSurfaces()
    {
        if (coatingMaterial == null)
        {
            Debug.LogError("BubbleClean needs a coating material.", this);
            return;
        }

        Shader brushShader = Shader.Find(
            "Hidden/AR-TT/Coating Mask Brush"
        );

        if (brushShader == null)
        {
            Debug.LogError("Coating mask brush Shader was not found.", this);
            return;
        }

        brushMaterial = new Material(brushShader);

        Renderer[] renderers =
            coatingRoot.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer coatingRenderer in renderers)
        {
            Mesh mesh = GetRendererMesh(coatingRenderer);
            if (mesh == null)
            {
                Debug.LogWarning(
                    $"Coating Renderer {coatingRenderer.name} has no mesh.",
                    coatingRenderer
                );
                continue;
            }

            Material materialInstance = new Material(coatingMaterial);
            RenderTexture mask = CreateMask(coatingRenderer.name);
            materialInstance.SetTexture("_CleanMask", mask);

            MeshCollider surfaceCollider =
                coatingRenderer.GetComponent<MeshCollider>();

            if (surfaceCollider == null)
            {
                surfaceCollider =
                    coatingRenderer.gameObject.AddComponent<MeshCollider>();
            }

            surfaceCollider.sharedMesh = mesh;
            surfaceCollider.convex = false;
            surfaceCollider.isTrigger = false;

            int cleanableLayer = LayerMask.NameToLayer("Cleanable");
            if (cleanableLayer >= 0)
            {
                coatingRenderer.gameObject.layer = cleanableLayer;
            }

            Material[] materials = coatingRenderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = materialInstance;
            }

            coatingRenderer.sharedMaterials = materials;

            CoatingSurface surface = new CoatingSurface
            {
                Renderer = coatingRenderer,
                Mesh = mesh,
                Collider = surfaceCollider,
                Material = materialInstance,
                Mask = mask
            };

            coatingSurfaces.Add(surface);
            surfacesByCollider[surfaceCollider] = surface;
        }
    }

    private void PaintStroke(CoatingSurface surface, Vector2 uv)
    {
        if (!surface.HasLastUv)
        {
            PaintMask(surface.Mask, uv, uv);
            surface.LastUv = uv;
            surface.HasLastUv = true;
            return;
        }

        if (Vector2.Distance(surface.LastUv, uv) > 0.25f)
        {
            // Avoid drawing a long line when crossing a UV seam.
            PaintMask(surface.Mask, uv, uv);
        }
        else
        {
            PaintMask(surface.Mask, surface.LastUv, uv);
        }

        surface.LastUv = uv;
    }

    private void PaintMask(
        RenderTexture mask,
        Vector2 startUv,
        Vector2 endUv)
    {
        brushMaterial.SetVector("_BrushStart", startUv);
        brushMaterial.SetVector("_BrushEnd", endUv);
        brushMaterial.SetFloat("_BrushRadius", maskBrushRadius);

        RenderTexture temporary = RenderTexture.GetTemporary(
            mask.width,
            mask.height,
            0,
            mask.format,
            RenderTextureReadWrite.Linear
        );

        Graphics.Blit(mask, temporary, brushMaterial);
        Graphics.Blit(temporary, mask);
        RenderTexture.ReleaseTemporary(temporary);
    }

    private RenderTexture CreateMask(string rendererName)
    {
        RenderTexture mask = new RenderTexture(
            maskResolution,
            maskResolution,
            0,
            RenderTextureFormat.R8,
            RenderTextureReadWrite.Linear
        )
        {
            name = $"{rendererName}_CleanMask",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        mask.Create();
        ClearMask(mask);
        return mask;
    }

    private void ResetCleanMasks()
    {
        EndCleanStroke();

        foreach (CoatingSurface surface in coatingSurfaces)
        {
            surface.HasLastUv = false;
            ClearMask(surface.Mask);
        }
    }

    private static void ClearMask(RenderTexture mask)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = mask;
        GL.Clear(false, true, Color.white);
        RenderTexture.active = previous;
    }

    private static Mesh GetRendererMesh(Renderer targetRenderer)
    {
        if (targetRenderer.TryGetComponent(out MeshFilter meshFilter))
        {
            return meshFilter.sharedMesh;
        }

        if (targetRenderer is SkinnedMeshRenderer skinnedRenderer)
        {
            return skinnedRenderer.sharedMesh;
        }

        return null;
    }

    private void WarnInvalidUvCollider(Collider targetCollider)
    {
        if (!invalidUvColliders.Add(targetCollider))
        {
            return;
        }

        Debug.LogWarning(
            $"Cleanable object {targetCollider.name} needs a MeshCollider " +
            "with a UV-mapped mesh for Render Texture cleaning.",
            targetCollider
        );
    }

    private void OnDestroy()
    {
        foreach (CoatingSurface surface in coatingSurfaces)
        {
            if (surface.Mask != null)
            {
                surface.Mask.Release();
                Destroy(surface.Mask);
            }

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
}
