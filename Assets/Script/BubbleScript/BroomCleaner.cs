using UnityEngine;

public class BroomCleaner : MonoBehaviour
{
    [SerializeField] private BubbleClean bubbleClean;
    [SerializeField] private LayerMask cleanableLayers;

    private void OnTriggerStay(Collider other)
    {
        if ((cleanableLayers.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        if (other is not MeshCollider)
        {
            return;
        }

        Vector3 closestPoint = other.ClosestPoint(transform.position);
        Vector3 direction = closestPoint - transform.position;
        float distance = direction.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            return;
        }

        Ray ray = new Ray(transform.position, direction / distance);
        if (other.Raycast(ray, out RaycastHit hit, distance + 0.05f))
        {
            bubbleClean.CleanAt(hit);
        }
    }
}
