using Fusion;
using UnityEngine;

public class BoxSpawner : NetworkBehaviour, IInteractable
{
    public string boxName = "Box";
    public NetworkPrefabRef boxPrefab;
    public Transform spawnPoint;

    public string GetInteractionPrompt()
    {
        return $"Press E to instantiate {boxName}";
    }

    public void OnInteract(PlayerRef interactor)
    {
        RPC_SpawnBox();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SpawnBox()
    {
        if (boxPrefab.IsValid)
        {
            Runner.Spawn(boxPrefab, spawnPoint.position, spawnPoint.rotation);
        }
    }
}
