using Fusion;
using System.Linq;
using UnityEngine;

public class RestZone : NetworkBehaviour, IInteractable
{
    public WorldPromptUI worldPrompt;

    [Networked, Capacity(4)]
    public NetworkLinkedList<PlayerRef> RestingPlayers => default;

    public string GetInteractionPrompt()
    {
        if (DayManager.Instance.CurrentPhase == DayPhase.Morning)
        {
            return "Rest Zone\n<color=#FFA500>Cannot rest during the morning (you can start the shift with C for now)</color>";
        }

        if (DayManager.Instance.CurrentPhase == DayPhase.ActiveShift)
        {
            return "Rest Zone\n<color=#FFA500>Cannot rest during the shift</color>";
        }

        if (RestingPlayers.Contains(Runner.LocalPlayer))
        {
            return $"Press [X] to cancel resting ({RestingPlayers.Count}/{Runner.ActivePlayers.Count()} ready)";
        }

        return $"Press [X] to rest and end the day ({RestingPlayers.Count}/{Runner.ActivePlayers.Count()} ready)";
    }

    public void OnInteract(PlayerRef interactor)
    {
        if (DayManager.Instance.CurrentPhase != DayPhase.Night)
        {
            return;
        }

        RPC_ToggleRest(interactor);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ToggleRest(PlayerRef interactor)
    {
        if (DayManager.Instance.CurrentPhase != DayPhase.Night)
        {
            return;
        }

        if (RestingPlayers.Contains(interactor))
        {
            RestingPlayers.Remove(interactor);
        }
        else
        {
            RestingPlayers.Add(interactor);
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
            RPC_ShowRestMessage("<color=#44FF44>All players resting! Good morning...</color>");
            DayManager.Instance.ChangePhase(DayPhase.Morning);
            RestingPlayers.Clear();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowRestMessage(string msg)
    {
        worldPrompt.ShowTemporaryMessage(msg, 3f);
    }
}