using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BoxChecklistUI : MonoBehaviour
{
    [Header("Detection")]
    public float lookDistance = 3.5f;
    public string boxTag = "Box";

    [Header("UI References")]
    public GameObject panelContainer;
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI itemsText;
    public TextMeshProUGUI statusText;

    private void Start()
    {
        if (panelContainer != null)
        {
            panelContainer.SetActive(false);
        }
    }

    private void Update()
    {
        Camera cam = Camera.main;
        if (cam == null || OrderManager.Instance == null)
        {
            HidePanel();
            return;
        }

        // Cast a ray from the center of the local player's camera
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, lookDistance))
        {
            BoxContainer box = hit.collider.GetComponentInParent<BoxContainer>();

            // Check if the parent box has the required tag and an attached stamp
            if (box != null && box.CompareTag(boxTag) && box.HasStamp)
            {
                DeliverablePackage package = box.GetComponentInParent<DeliverablePackage>();
                if (package != null && package.IsDelivered)
                {
                    HidePanel();
                    return;
                }

                ShowChecklistForBox(box);
                return;
            }
        }

        HidePanel();
    }

    private void ShowChecklistForBox(BoxContainer box)
    {
        if (panelContainer != null && !panelContainer.activeSelf)
        {
            panelContainer.SetActive(true);
        }

        string stampContent = box.StampContent.ToString().Trim();
        OrderData matchedOrder = FindMatchingOrder(stampContent);

        if (matchedOrder == null)
        {
            headerText.text = $"<b>Destination:</b> {stampContent}";
            itemsText.text = "<color=#FF4444>No active order for this destination.</color>";
            statusText.text = "";
            return;
        }

        headerText.text = $"<b>Destination:</b> {matchedOrder.destination.destiny}\n<color=#44FF44>Payment: ${matchedOrder.payment}</color>";

        // Copy networked item IDs to track matched and extra items
        List<int> boxItems = new List<int>();
        foreach (int id in box.PackedItemIDs)
        {
            boxItems.Add(id);
        }

        string checklist = "";
        int totalMissing = 0;

        foreach (ProductQuantity line in matchedOrder.products)
        {
            int foundCount = 0;

            for (int i = 0; i < line.quantity; i++)
            {
                if (boxItems.Contains(line.product.id))
                {
                    boxItems.Remove(line.product.id);
                    foundCount++;
                }
                else
                {
                    totalMissing++;
                }
            }

            if (foundCount >= line.quantity)
            {
                checklist += $"<color=#44FF44>[X] <s>{line.product.productName} ({foundCount}/{line.quantity})</s></color>\n";
            }
            else if (foundCount > 0)
            {
                checklist += $"<color=#FFFF00>[ ] {line.product.productName} ({foundCount}/{line.quantity})</color>\n";
            }
            else
            {
                checklist += $"[ ] {line.product.productName} (0/{line.quantity})\n";
            }
        }

        itemsText.text = checklist.TrimEnd('\n');

        int extraItems = boxItems.Count;
        if (totalMissing == 0 && extraItems == 0)
        {
            statusText.text = "<color=#44FF44><b>READY TO DELIVER</b></color>";
        }
        else if (extraItems > 0)
        {
            statusText.text = $"<color=#FFA500>Warning: {extraItems} extra item(s) in box</color>";
        }
        else
        {
            statusText.text = $"<color=#CCCCCC>Missing {totalMissing} item(s)</color>";
        }
    }

    private OrderData FindMatchingOrder(string stampContent)
    {
        var orders = OrderManager.Instance.ReceivedOrders;
        for (int i = 0; i < orders.Count; i++)
        {
            OrderData candidate = OrderManager.Instance.GetOrder(orders[i]);
            if (candidate != null && candidate.destination != null)
            {
                if (string.Equals(candidate.destination.destiny.Trim(), stampContent, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }
        }
        return null;
    }

    private void HidePanel()
    {
        if (panelContainer != null && panelContainer.activeSelf)
        {
            panelContainer.SetActive(false);
        }
    }
}