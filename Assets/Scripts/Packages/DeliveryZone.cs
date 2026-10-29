using Fusion;
using UnityEngine;

public class DeliveryZone : NetworkBehaviour
{
    public Vector3 zoneExtents = new Vector3(2f, 1f, 2f);
    public LayerMask boxLayer;
    public Transform placementPoint;
    public Vector3 boxSpacing = new Vector3(0.6f, 0f, 0.6f);
    public int boxesPerRow = 3;
    [Networked] public int DeliveredCount { get; set; }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        Collider[] hits = Physics.OverlapBox(transform.position, zoneExtents, transform.rotation, boxLayer);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out DeliverablePackage package))
            {
                if (!package.Grabbable.IsGrabbed && !package.IsDelivered)
                {
                    ProcessDelivery(package);
                }
            }
        }
    }

    private void ProcessDelivery(DeliverablePackage package)
    {
        package.IsDelivered = true;

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddRevenue(package.rewardValue);
        }

        Rigidbody rb = package.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (placementPoint != null)
        {
            int col = DeliveredCount % boxesPerRow;
            int row = DeliveredCount / boxesPerRow;
            Vector3 localOffset = new Vector3(col * boxSpacing.x, 0, row * boxSpacing.z);
            package.transform.position = placementPoint.position + placementPoint.TransformDirection(localOffset);
            package.transform.rotation = placementPoint.rotation;
        }

        DeliveredCount++;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.4f);
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, zoneExtents * 2);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, zoneExtents * 2);
    }
}