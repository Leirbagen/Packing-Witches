using Fusion;
using UnityEngine;
using System.Collections;

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

    [Networked, Capacity(50)]
    public NetworkLinkedList<int> PendingOrders => default;

    [Networked]
    private DayPhase previousPhase { get; set; }

    public void OrderSupply(int index, bool isExpress)
    {
        RPC_RequestOrderSupply(index, isExpress);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestOrderSupply(int index, NetworkBool isExpress)
    {
        if (index < 0 || index >= supplierCatalog.Length) return;

        SupplierEntry entry = supplierCatalog[index];
        if (entry.productData == null) return;

        int finalPrice = isExpress ? (int)entry.productData.price * 2 : (int)entry.productData.price;

        if (EconomyManager.Instance.TrySpend(finalPrice))
        {
            RPC_BroadcastOrder(index, isExpress, true);

            if (isExpress)
            {
                StartCoroutine(ExpressDeliveryRoutine(entry));
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
        int finalPrice = isExpress ? (int)entry.productData.price * 2 : (int)entry.productData.price;
        string type = isExpress ? "EXPRESS" : "NORMAL";

        if (success)
        {
            Debug.Log($"<color=yellow>[Supplier] {type} Order placed: {entry.productData.productName} for ${finalPrice}.</color>");
        }
        else
        {
            Debug.LogWarning($"[Supplier] Not enough money to order {entry.productData.productName}. Need ${finalPrice}.");
        }
    }

    private IEnumerator ExpressDeliveryRoutine(SupplierEntry entry)
    {
        yield return new WaitForSeconds(expressDeliveryDelay);

        if (Object != null && Object.HasStateAuthority)
        {
            Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0f, 1f), Random.Range(-0.5f, 0.5f));
            Runner.Spawn(entry.prefab, storageSpawnPoint.position + randomOffset, storageSpawnPoint.rotation);

            RPC_BroadcastDelivery(entry.productData.productName, true);
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
        if (!HasStateAuthority || DayManager.Instance == null) return;

        DayPhase currentPhase = DayManager.Instance.CurrentPhase;

        if (previousPhase != DayPhase.Morning && currentPhase == DayPhase.Morning)
        {
            DeliverPendingOrders();
        }

        previousPhase = currentPhase;
    }

    private void DeliverPendingOrders()
    {
        if (PendingOrders.Count == 0) return;

        RPC_BroadcastDelivery($"{PendingOrders.Count} items", false);

        foreach (int index in PendingOrders)
        {
            if (index >= 0 && index < supplierCatalog.Length)
            {
                SupplierEntry entry = supplierCatalog[index];
                Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0f, 1f), Random.Range(-0.5f, 0.5f));
                Runner.Spawn(entry.prefab, storageSpawnPoint.position + randomOffset, storageSpawnPoint.rotation);
            }
        }
        PendingOrders.Clear();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastDelivery(string deliveryName, NetworkBool isExpress)
    {
        if (isExpress)
        {
            Debug.Log($"<color=orange>[Supplier] EXPRESS delivery arrived: {deliveryName}!</color>");
        }
        else
        {
            Debug.Log($"<color=green>[Supplier] Good morning! {deliveryName} delivered to storage.</color>");
        }
    }
}