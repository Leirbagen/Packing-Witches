using Fusion;
using UnityEngine;

public class ProductDisplayZone : NetworkBehaviour
{
    [Header("Display Area")]
    public Vector3 zoneExtents = new Vector3(0.5f, 0.5f, 0.5f);
    public LayerMask itemLayer;

    public PackableItem GetDisplayedItem()
    {
        Collider[] hits = Physics.OverlapBox(transform.position, zoneExtents, transform.rotation, itemLayer);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out PackableItem item) && !item.IsPacked)
            {
                if (item.TryGetComponent(out GrabbableObject grabbable) && !grabbable.IsGrabbed)
                {
                    return item;
                }
            }
        }

        return null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.8f, 0.2f, 0.8f, 0.4f);
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, zoneExtents * 2);
    }
}