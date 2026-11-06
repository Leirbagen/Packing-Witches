using Fusion;
using System.Linq;
using UnityEngine;

public class RestZone : NetworkBehaviour, IInteractable
{
    [Networked, Capacity(4)]
    public NetworkLinkedList<PlayerRef> RestingPlayers => default;

    public string GetInteractionPrompt()
    {
        if (DayManager.Instance.CurrentPhase != DayPhase.Night)
            return string.Empty;

        if (RestingPlayers.Contains(Runner.LocalPlayer))
        {
            return $"Press [X] to cancel resting ({RestingPlayers.Count}/{Runner.ActivePlayers.Count()} ready)";
        }
        return $"Press [X] to rest and end the day ({RestingPlayers.Count}/{Runner.ActivePlayers.Count()} ready)";
    }

    public void OnInteract(PlayerRef interactor)
    {
        if (DayManager.Instance.CurrentPhase != DayPhase.Night) return;

        RPC_ToggleRest(interactor);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ToggleRest(PlayerRef interactor)
    {
        if (DayManager.Instance.CurrentPhase != DayPhase.Night) return;

        if (RestingPlayers.Contains(interactor))
        {
            RestingPlayers.Remove(interactor);
            Debug.Log($"[RestZone] Player {interactor.PlayerId} canceled rest. ({RestingPlayers.Count}/{Runner.ActivePlayers.Count()})");
        }
        else
        {
            RestingPlayers.Add(interactor);
            Debug.Log($"[RestZone] Player {interactor.PlayerId} is resting. ({RestingPlayers.Count}/{Runner.ActivePlayers.Count()})");

            CheckAllPlayersResting();
        }
    }

    private void CheckAllPlayersResting()
    {
        int activePlayersCount = 0;
        foreach (var player in Runner.ActivePlayers)
        {
            activePlayersCount++;
        }

        if (RestingPlayers.Count >= activePlayersCount)
        {
            Debug.Log("[RestZone] All players are resting! Transitioning to Morning...");

            DayManager.Instance.ChangePhase(DayPhase.Morning);

            RestingPlayers.Clear();
        }
    }
}