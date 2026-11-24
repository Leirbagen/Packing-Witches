using Fusion;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SupplierManager : NetworkBehaviour
{
    [System.Serializable]
    public class SupplierEntry
    {
        public ProductData productData;
        public NetworkPrefabRef prefab;
    }

    [Header("Catalog & Spawning")]
    public SupplierEntry[] supplierCatalog;
    public Transform storageSpawnPoint;
    public float expressDeliveryDelay = 3f;

    [Header("Delivery Box Settings")]
    public NetworkPrefabRef deliveryBoxPrefab;
    public int maxItemsPerBox = 4;
    public Vector3 boxSpacing = new Vector3(0.8f, 0f, 0.8f);
    public float itemVerticalSpacing = 0.12f;

    public WorldPromptUI worldPrompt;

    [Networked, Capacity(50)]
    public NetworkLinkedList<int> PendingOrders => default;

    [Networked]
    private DayPhase previousPhase { get; set; }

    private List<SupplierEntry> expressQueue = new List<SupplierEntry>();
    private Coroutine expressCoroutine;
    private int totalSpawnedBoxes = 0;

    public void OrderSupply(int index, bool isExpress)
    {
        RPC_RequestOrderSupply(index, isExpress);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestOrderSupply(int index, NetworkBool isExpress)
    {
        if (index < 0 || index >= supplierCatalog.Length)
        {
            return;
        }

        SupplierEntry entry = supplierCatalog[index];
        if (entry.productData == null)
        {
            return;
        }

        int finalPrice = (int)entry.productData.price;
        if (isExpress)
        {
            finalPrice = (int)entry.productData.price * 2;
        }

        if (EconomyManager.Instance.TrySpend(finalPrice))
        {
            RPC_BroadcastOrder(index, isExpress, true);

            if (isExpress)
            {
                expressQueue.Add(entry);
                if (expressCoroutine != null)
                {
                    StopCoroutine(expressCoroutine);
                }
                expressCoroutine = StartCoroutine(ExpressDeliveryRoutine());
            }
            else
            {
                PendingOrders.Add(index);
            }
        }
        else
        {
            RPC_BroadcastOrder(index, isExpress, false);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastOrder(int index, NetworkBool isExpress, NetworkBool success)
    {
        SupplierEntry entry = supplierCatalog[index];
        int finalPrice = (int)entry.productData.price;
        string type = "NORMAL";

        if (isExpress)
        {
            finalPrice = (int)entry.productData.price * 2;
            type = "EXPRESS";
        }

        if (success)
        {
            worldPrompt.ShowTemporaryMessage($"<color=#FFFF00>{type} Order: {entry.productData.productName} (${finalPrice})</color>");
        }
        else
        {
            worldPrompt.ShowTemporaryMessage($"<color=#FF4444>Not enough money! Need ${finalPrice}</color>");
        }
    }

    private IEnumerator ExpressDeliveryRoutine()
    {
        yield return new WaitForSeconds(expressDeliveryDelay);

        if (Object != null && Object.HasStateAuthority && expressQueue.Count > 0)
        {
            SpawnBatchedBoxes(expressQueue);
            RPC_BroadcastDelivery($"{expressQueue.Count} items", true);
            expressQueue.Clear();
            expressCoroutine = null;
        }
    }

    public override void Spawned()
    {
        if (HasStateAuthority && DayManager.Instance != null)
        {
            previousPhase = DayManager.Instance.CurrentPhase;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || DayManager.Instance == null)
        {
            return;
        }

        DayPhase currentPhase = DayManager.Instance.CurrentPhase;

        if (previousPhase != DayPhase.Morning && currentPhase == DayPhase.Morning)
        {
            DeliverPendingOrders();
        }

        previousPhase = currentPhase;
    }

    private void DeliverPendingOrders()
    {
        if (PendingOrders.Count == 0)
        {
            return;
        }

        RPC_BroadcastDelivery($"{PendingOrders.Count} items", false);

        List<SupplierEntry> itemsToDeliver = new List<SupplierEntry>();
        foreach (int index in PendingOrders)
        {
            if (index >= 0 && index < supplierCatalog.Length)
            {
                itemsToDeliver.Add(supplierCatalog[index]);
            }
        }

        SpawnBatchedBoxes(itemsToDeliver);
        PendingOrders.Clear();
    }

    private void SpawnBatchedBoxes(List<SupplierEntry> allItems)
    {
        List<SupplierEntry> batch = new List<SupplierEntry>();

        foreach (SupplierEntry entry in allItems)
        {
            batch.Add(entry);
            if (batch.Count >= maxItemsPerBox)
            {
                SpawnBoxWithItems(batch);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            SpawnBoxWithItems(batch);
        }
    }

    private void SpawnBoxWithItems(List<SupplierEntry> items)
    {
        if (!deliveryBoxPrefab.IsValid)
        {
            return;
        }

        int slot = totalSpawnedBoxes % 9;
        totalSpawnedBoxes++;

        int col = slot % 3;
        int row = slot / 3;
        Vector3 localOffset = new Vector3(col * boxSpacing.x, 0f, row * boxSpacing.z);
        Vector3 boxPosition = storageSpawnPoint.position + storageSpawnPoint.TransformDirection(localOffset);

        NetworkObject boxObj = Runner.Spawn(deliveryBoxPrefab, boxPosition, storageSpawnPoint.rotation);
        if (boxObj.TryGetComponent(out Rigidbody boxRb))
        {
            boxRb.linearVelocity = Vector3.zero;
            boxRb.angularVelocity = Vector3.zero;
        }

        BoxContainer box = boxObj.GetComponent<BoxContainer>();
        if (box == null)
        {
            box = boxObj.GetComponentInChildren<BoxContainer>();
        }

        Vector3 spawnCenter = boxPosition + new Vector3(0f, 0.15f, 0f);
        Vector3 zoneHalfSize = new Vector3(0.15f, 0.1f, 0.15f);

        if (box != null && box.contentZone != null)
        {
            zoneHalfSize = Vector3.Scale(box.contentZone.size, box.contentZone.transform.lossyScale) * 0.25f;
        }

        for (int i = 0; i < items.Count; i++)
        {
            int gridX = i % 2;
            int gridZ = (i / 2) % 2;
            int layerY = i / 4;

            float offsetX = -zoneHalfSize.x;
            if (gridX == 1)
            {
                offsetX = zoneHalfSize.x;
            }

            float offsetZ = -zoneHalfSize.z;
            if (gridZ == 1)
            {
                offsetZ = zoneHalfSize.z;
            }

            Vector3 localItemPos = new Vector3(offsetX, layerY * itemVerticalSpacing, offsetZ);
            Vector3 worldItemPos = spawnCenter + storageSpawnPoint.TransformDirection(localItemPos);

            NetworkObject itemObj = Runner.Spawn(items[i].prefab, worldItemPos, storageSpawnPoint.rotation);
            if (itemObj.TryGetComponent(out Rigidbody itemRb))
            {
                itemRb.linearVelocity = Vector3.zero;
                itemRb.angularVelocity = Vector3.zero;
            }
        }

        if (box != null)
        {
            box.RefreshContents();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastDelivery(string deliveryName, NetworkBool isExpress)
    {
        if (isExpress)
        {
            worldPrompt.ShowTemporaryMessage($"<color=#FFA500>EXPRESS arrived: {deliveryName}!</color>", 3.5f);
        }
        else
        {
            worldPrompt.ShowTemporaryMessage($"<color=#44FF44>Good morning! {deliveryName} in storage.</color>", 4f);
        }
    }
}