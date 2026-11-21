using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class DeliveryZone : NetworkBehaviour, IInteractable
{
    [Header("Delivery Zone")]
    public Vector3 zoneExtents = new Vector3(2f, 1f, 2f);
    public LayerMask boxLayer;

    [Header("Box Placement")]
    public Transform placementPoint;
    public Vector3 boxSpacing = new Vector3(0.6f, 0f, 0.6f);
    public int boxesPerRow = 3;

    [Header("UI")]
    public WorldPromptUI worldPrompt;

    [Networked]
    public int DeliveredCount { get; set; }

    [Networked]
    private TickTimer emptyWarningTimer { get; set; }

    public string GetInteractionPrompt()
    {
        if (DayManager.Instance == null)
        {
            return "Delivery Zone";
        }

        if (DayManager.Instance.CurrentPhase == DayPhase.ActiveShift)
        {
            return "Delivery Zone\n<color=#44FF44>Drop stamped boxes here</color>";
        }

        if (DayManager.Instance.CurrentPhase == DayPhase.Morning)
        {
            return "Delivery Zone\n<color=#FFA500>Closed (Start the shift first)</color>";
        }

        return "Delivery Zone\n<color=#FF4444>Closed (Shift ended)</color>";
    }

    public void OnInteract(PlayerRef interactor)
    {
        // Delivery is handled automatically when dropping boxes inside the zone
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
        {
            return;
        }

        if (DayManager.Instance == null || DayManager.Instance.CurrentPhase != DayPhase.ActiveShift)
        {
            return;
        }

        Collider[] hits = Physics.OverlapBox(transform.position, zoneExtents, transform.rotation, boxLayer, QueryTriggerInteraction.Collide);

        foreach (Collider hit in hits)
        {
            DeliverablePackage package = hit.GetComponentInParent<DeliverablePackage>();

            if (package == null)
            {
                continue;
            }
            if (package.Grabbable == null)
            {
                continue;
            }
            if (package.Grabbable.IsGrabbed || package.IsDelivered)
            {
                continue;
            }

            ProcessDelivery(package);
        }
    }

    private void ProcessDelivery(DeliverablePackage package)
    {
        if (!HasStateAuthority || package.IsDelivered)
        {
            return;
        }

        BoxContainer box = package.GetComponent<BoxContainer>();
        if (box == null)
        {
            box = package.GetComponentInChildren<BoxContainer>();
        }

        if (box != null)
        {
            box.RefreshContents();

            // Do not allow delivery if the box is completely empty
            if (box.PackedItemIDs.Count == 0)
            {
                if (emptyWarningTimer.ExpiredOrNotRunning(Runner))
                {
                    emptyWarningTimer = TickTimer.CreateFromSeconds(Runner, 2.5f);
                    RPC_ShowDeliveryMessage("<color=#FF4444>Box is empty! Add items before delivering</color>");
                }
                return;
            }
        }

        package.IsDelivered = true;

        if (box != null)
        {
            int payment = CheckBox(box, out int stars, out int discount);

            if (payment > 0 && EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddRevenue(payment);
            }

            if (ReputationManager.Instance != null)
            {
                ReputationManager.Instance.AddRating(stars);
            }

            if (payment > 0)
            {
                if (discount > 0)
                {
                    RPC_ShowDeliveryMessage($"<color=#FFA500>Incomplete! +${payment} (-${discount})</color>");
                }
                else
                {
                    RPC_ShowDeliveryMessage($"<color=#44FF44>Perfect Delivery! +${payment} ({stars} stars)</color>");
                }
            }

            box.PrepareForDelivery();
        }

        PlaceBox(package);
    }

    private int CheckBox(BoxContainer box, out int stars, out int discount)
    {
        stars = 1;
        discount = 0;

        if (!box.HasStamp)
        {
            RPC_ShowDeliveryMessage("<color=#FF4444>Missing stamp! ($0)</color>");
            return 0;
        }

        if (OrderManager.Instance == null)
        {
            return 0;
        }

        var orders = OrderManager.Instance.ReceivedOrders;

        if (orders.Count == 0)
        {
            RPC_ShowDeliveryMessage("<color=#FF4444>No pending orders</color>");
            return 0;
        }

        OrderData order = null;
        int orderId = -1;

        string stampContent = box.StampContent.ToString().Trim();
        for (int i = 0; i < orders.Count; i++)
        {
            OrderData candidate = OrderManager.Instance.GetOrder(orders[i]);
            if (candidate != null && candidate.destination != null && string.Equals(candidate.destination.destiny.Trim(), stampContent, System.StringComparison.OrdinalIgnoreCase))
            {
                order = candidate;
                orderId = orders[i];
                break;
            }
        }

        if (order == null)
        {
            RPC_ShowDeliveryMessage("<color=#FF4444>No order for this destination ($0)</color>");
            return 0;
        }

        List<int> boxItems = new List<int>();
        foreach (int id in box.PackedItemIDs)
        {
            boxItems.Add(id);
        }

        int totalRequired = 0;
        int matched = 0;

        foreach (ProductQuantity line in order.products)
        {
            totalRequired += line.quantity;

            for (int i = 0; i < line.quantity; i++)
            {
                if (boxItems.Contains(line.product.id))
                {
                    boxItems.Remove(line.product.id);
                    matched++;
                }
            }
        }

        int extra = boxItems.Count;
        orders.Remove(orderId);

        int fullPay = Mathf.RoundToInt((float)order.payment);

        if (totalRequired <= 0 || matched == 0)
        {
            stars = 1;
            discount = fullPay;
            RPC_ShowDeliveryMessage($"<color=#FF4444>Wrong items! -$ {fullPay} ($0 earned)</color>");
            return 0;
        }

        // Calculate proportional payment based on delivered items
        float completionRatio = (float)matched / totalRequired;
        int earnedPay = Mathf.RoundToInt(fullPay * completionRatio);

        // Apply a small penalty per wrong/extra item included
        if (extra > 0)
        {
            int extraPenalty = Mathf.RoundToInt((fullPay / (float)totalRequired) * 0.25f * extra);
            earnedPay = Mathf.Max(0, earnedPay - extraPenalty);
        }

        discount = fullPay - earnedPay;

        if (matched == totalRequired && extra == 0)
        {
            stars = 5;
        }
        else if (completionRatio >= 0.5f)
        {
            stars = 3;
        }
        else
        {
            stars = 2;
        }

        return earnedPay;
    }

    private void PlaceBox(DeliverablePackage package)
    {
        Rigidbody rb = package.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (placementPoint != null)
        {
            int col = DeliveredCount % Mathf.Max(1, boxesPerRow);
            int row = DeliveredCount / Mathf.Max(1, boxesPerRow);
            Vector3 localOffset = new Vector3(col * boxSpacing.x, 0f, row * boxSpacing.z);

            package.transform.SetPositionAndRotation(
                placementPoint.position + placementPoint.TransformDirection(localOffset),
                placementPoint.rotation);
        }

        DeliveredCount++;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowDeliveryMessage(string msg)
    {
        if (worldPrompt != null)
        {
            worldPrompt.ShowTemporaryMessage(msg, 3f);
        }

        if (DeliveryFeedbackUI.Instance != null)
        {
            DeliveryFeedbackUI.Instance.ShowMessage(msg, 3.5f);
        }
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