using System.Collections.Generic;
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
        if (!HasStateAuthority)
        {
            return;
        } 

        Collider[] hits = Physics.OverlapBox(transform.position, zoneExtents, transform.rotation, boxLayer);
        foreach (var hit in hits)
        {
            DeliverablePackage package = hit.GetComponentInParent<DeliverablePackage>();
            if (!package.Grabbable.IsGrabbed && !package.IsDelivered)
            {
                ProcessDelivery(package);
            }
        }
    }

    private void ProcessDelivery(DeliverablePackage package)
    {
        package.IsDelivered = true;

        BoxContainer box = package.GetComponent<BoxContainer>();
        if (box == null)
        {
            box = package.GetComponentInChildren<BoxContainer>();
        } 
        if (box != null)
        {
            int payment = CheckBox(box, out int stars);
            if (payment > 0 && EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddRevenue(payment);
            }
            if (ReputationManager.Instance != null)
            {
                ReputationManager.Instance.AddRating(stars);
            }
            Debug.Log("[Delivery] Pago: " + payment);
        }
        PlaceBox(package);
    }

    private int CheckBox(BoxContainer box, out int stars)
    {
        stars = 1;
        if (!box.HasStamp)
        {
            Debug.Log("[Delivery] La caja no tiene estampa");
            return 0;
        }
        var orders = OrderManager.Instance.ReceivedOrders;
        OrderData order = null;
        int orderId = -1;
        for (int i = 0; i < orders.Count; i++)
        {
            OrderData candidate = OrderManager.Instance.GetOrder(orders[i]);
            if (candidate.destination.id == box.StampDestinationId)
            {
                order = candidate;
                orderId = orders[i];
                break;
            }
        }
        if (order == null)
        {
            return 0;
        } 
        List<int> boxItems = new List<int>();
        foreach (int id in box.PackedItemIDs)
        {
            boxItems.Add(id);
        }
        int missing = 0;
        foreach (ProductQuantity line in order.products)
        {
            for (int i = 0; i < line.quantity; i++)
            {
                if (boxItems.Contains(line.product.id))
                {
                    boxItems.Remove(line.product.id);
                }
                else
                {
                    missing++;
                }
            }
        }
        int extra = boxItems.Count;
        int errors = Mathf.Max(missing, extra);
        orders.Remove(orderId);
        int fullPay = Mathf.RoundToInt((float)order.payment);
        if (errors == 0)
        {
            stars = 5;
            return fullPay;
        } 
        if (errors == 1)
        {
            stars = 3;
            return fullPay / 2;
        }
        stars = 1;
        return 0;
    }

    private void PlaceBox(DeliverablePackage package)
    {
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