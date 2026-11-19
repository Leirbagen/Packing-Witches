using Fusion;
using UnityEngine;

public class CatBowl : NetworkBehaviour, IInteractable
{
    public CatController cat; 

    public string GetInteractionPrompt()
    {
        return "Press [F] to fill food bowl";
    }

    public void OnInteract(PlayerRef interactor)
    {
        RPC_FillBowl();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_FillBowl()
    {
        if (cat != null)
        {
            cat.FeedCat();
        }
    }
}