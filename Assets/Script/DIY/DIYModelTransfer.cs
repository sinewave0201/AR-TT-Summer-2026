using System.Collections.Generic;
using UnityEngine;

// Holds the generated DIY model between scene loads.
public sealed class DIYModelTransfer : MonoBehaviour
{
    public static DIYModelTransfer Current { get; private set; }
    public GameObject Model => gameObject;

    private List<Material> ownedMaterials;
    private List<Texture2D> ownedTextures;

    public static void Store(
        GameObject model,
        List<Material> materials,
        List<Texture2D> textures)
    {
        if (Current != null)
        {
            Destroy(Current.gameObject);
        }

        DIYModelTransfer transfer = model.AddComponent<DIYModelTransfer>();
        transfer.ownedMaterials = materials;
        transfer.ownedTextures = textures;
        Current = transfer;

        DontDestroyOnLoad(model);
        model.SetActive(false);
    }

    private void OnDestroy()
    {
        // Instantiated avatar copies also contain this component. Only the
        // original transfer object owns and may destroy the saved resources.
        if (Current != this)
        {
            return;
        }


        Current = null;

        if (ownedMaterials != null)
        {
            foreach (Material material in ownedMaterials)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }
        }

        if (ownedTextures != null)
        {
            foreach (Texture2D texture in ownedTextures)
            {
                if (texture != null)
                {
                    Destroy(texture);
                }
            }
        }
    }
}
