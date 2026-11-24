using Fusion;
using UnityEngine;

public class StampPrinter : NetworkBehaviour
{
    [SerializeField] private NetworkPrefabRef stampPrefab;
    [SerializeField] private Transform printPoint;
    [SerializeField] private StampRoll roll;

    public void RequestPrint(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            Debug.LogWarning("[Printer] Empty stamp content.");
            return;
        }

        RPC_RequestPrint(content.Trim());
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestPrint(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }
        if (printPoint == null || !stampPrefab.IsValid)
        {
            return;
        }
        if(!roll.TryConsume())
        {
            return;
        }
        NetworkObject spawnedObject = Runner.Spawn(stampPrefab, printPoint.position, printPoint.rotation);
        if (spawnedObject == null)
        {
            return;
        }
        Stamp stamp = spawnedObject.GetComponent<Stamp>();
        if (stamp != null)
        {
            stamp.SetContent(content);
        }
    }
}
