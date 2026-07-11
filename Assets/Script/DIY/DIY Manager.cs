using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DIYManager : MonoBehaviour
{
    private sealed class ModelSurface
    {
        public Renderer Renderer;
        public Mesh Mesh;
        public MeshCollider Collider;
        public Material Material;
        public RenderTexture Mask;
        public Vector2 LastUv;
        public bool HasLastUv;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

}
