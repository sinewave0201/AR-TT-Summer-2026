using UnityEngine;

public class BubbleBurn : MonoBehaviour
{
    [SerializeField] private string fireplaceTag = "Fireplace";
    [SerializeField] private GameObject healEffect;
    [SerializeField] private GameObject bubbleRootToDisable;

    private bool burnEnabled;
    private bool burned;
    private AudioSource audioSource;

    private void Awake()
    {
        if (bubbleRootToDisable == null)
        {
            bubbleRootToDisable = gameObject;
        }

        if (healEffect == null)
        {
            Transform foundEffect = FindChildContaining(transform, "heal");
            if (foundEffect != null)
            {
                healEffect = foundEffect.gameObject;
            }
        }
    }

    public void EnableBurn()
    {
        burnEnabled = true;
        burned = false;
    }

    public void DisableBurn()
    {
        burnEnabled = false;
    }

    public void ResetBurnState()
    {
        burned = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryBurn(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryBurn(other.gameObject);
    }

    private void TryBurn(GameObject other)
    {
        if (!burnEnabled || burned || !IsFireplace(other))
        {
            return;
        }

        burned = true;
        PlayHealEffect();
        bubbleRootToDisable.SetActive(false);

        audioSource = other.GetComponent<AudioSource>();
        audioSource.Play();
    }

    private bool IsFireplace(GameObject other)
    {
        bool tagMatches = !string.IsNullOrEmpty(fireplaceTag) && other.tag == fireplaceTag;

        return tagMatches
            || other.name.ToLowerInvariant().Contains("fireplace");
    }

    private void PlayHealEffect()
    {
        if (healEffect == null)
        {
            return;
        }

        GameObject effectInstance = Instantiate(healEffect, transform.position, healEffect.transform.rotation);
        effectInstance.SetActive(true);

        ParticleSystem[] particles = effectInstance.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem particle in particles)
        {
            particle.Play();
        }
    }

    private Transform FindChildContaining(Transform root, string text)
    {
        string lowerText = text.ToLowerInvariant();

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child != root && child.name.ToLowerInvariant().Contains(lowerText))
            {
                return child;
            }
        }

        return null;
    }
}
