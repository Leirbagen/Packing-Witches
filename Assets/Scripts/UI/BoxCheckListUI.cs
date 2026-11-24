using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BoxChecklistUI : MonoBehaviour
{
    [Header("Detection")]
    public float lookDistance = 3.5f;
    public float detectionRadius = 0.08f;
    public string boxTag = "Box";

    [Header("UI References")]
    public GameObject panelContainer;
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI itemsText;
    public TextMeshProUGUI statusText;

    [Header("Smooth Transition")]
    public float fadeSpeed = 8f;
    public float hideDelay = 0.25f;

    private CanvasGroup canvasGroup;
    private BoxContainer currentBox;
    private float hideTimer = 0f;
    private bool isVisible = false;

    private void Start()
    {
        if (panelContainer != null)
        {
            canvasGroup = panelContainer.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panelContainer.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            panelContainer.SetActive(false);
        }
    }

    private void Update()
    {
        BoxContainer detectedBox = DetectLookedBox();

        if (detectedBox != null)
        {
            currentBox = detectedBox;
            hideTimer = hideDelay;
            isVisible = true;
            UpdateChecklistContent(currentBox);
        }
        else
        {
            if (hideTimer > 0f)
            {
                hideTimer -= Time.deltaTime;
                if (currentBox != null)
                {
                    UpdateChecklistContent(currentBox);
                }
            }
            else
            {
                isVisible = false;
                currentBox = null;
            }
        }

        UpdatePanelFade();
    }

    private BoxContainer DetectLookedBox()
    {
        Camera cam = Camera.main;
        if (cam == null || OrderManager.Instance == null)
        {
            return null;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.SphereCast(ray, detectionRadius, out RaycastHit hit, lookDistance))
        {
            BoxContainer box = hit.collider.GetComponentInParent<BoxContainer>();

            if (box != null && box.CompareTag(boxTag) && box.HasStamp)
            {
                DeliverablePackage package = box.GetComponentInParent<DeliverablePackage>();
                if (package != null && package.IsDelivered)
                {
                    return null;
                }

                return box;
            }
        }

        return null;
    }

    private void UpdatePanelFade()
    {
        if (panelContainer == null || canvasGroup == null)
        {
            return;
        }

        float targetAlpha = 0f;
        if (isVisible)
        {
            targetAlpha = 1f;
            if (!panelContainer.activeSelf)
            {
                panelContainer.SetActive(true);
            }
        }

        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);

        if (!isVisible && canvasGroup.alpha <= 0.01f)
        {
            canvasGroup.alpha = 0f;
            if (panelContainer.activeSelf)
            {
                panelContainer.SetActive(false);
            }
        }
    }

    private void UpdateChecklistContent(BoxContainer box)
    {
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
}