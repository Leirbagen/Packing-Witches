using Fusion;
using Photon;
using UnityEngine;

public class ShopSpawner : NetworkBehaviour
{
    [System.Serializable]
    public class ShopEntry
    {
        public string name;
        public int price;
        public NetworkPrefabRef prefab;
        public Transform spawnPoint;
    }

    public ShopEntry[] entries;

    public void Buy(int index)
    {
        RPC_Buy(index);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_Buy(int index)
    {
        if (index < 0 || index >= entries.Length)
        {
            return;
        } 
        ShopEntry entry = entries[index];
        if (EconomyManager.Instance.TrySpend(entry.price))
        {
            Runner.Spawn(entry.prefab, entry.spawnPoint.position, entry.spawnPoint.rotation);
        } 
    }
}
