using Fusion;
using UnityEngine;
using UnityEngine.AI;

public enum CatState { Idle, Annoying }

[RequireComponent(typeof(NavMeshAgent))]
public class CatController : NetworkBehaviour, IInteractable
{
    [Header("Cat Settings")]
    public float timeUntilHungry = 120f;
    public Transform annoyTargetPoint;

    [Networked] public CatState CurrentState { get; set; }
    [Networked] public TickTimer HungerTimer { get; set; }

    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            CurrentState = CatState.Idle;
            HungerTimer = TickTimer.CreateFromSeconds(Runner, timeUntilHungry);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        switch (CurrentState)
        {
            case CatState.Idle:
                if (HungerTimer.Expired(Runner))
                {
                    CurrentState = CatState.Annoying;
                    RPC_BroadcastCatMessage("<color=red>Cat is hungry</color>");

                    if (annoyTargetPoint != null)
                    {
                        agent.SetDestination(annoyTargetPoint.position);
                    }
                }
                break;

            case CatState.Annoying:
                
                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                {
                }
                break;
        }
    }

    public void FeedCat()
    {
        if (!HasStateAuthority) return;

        CurrentState = CatState.Idle;
        HungerTimer = TickTimer.CreateFromSeconds(Runner, timeUntilHungry);
        RPC_BroadcastCatMessage("<color=green>Cat is eating.</color>");
    }

    public string GetInteractionPrompt()
    {
        if (CurrentState == CatState.Annoying)
        {
            return "Cat is angry! (Go fill the food bowl)";
        }

        return "Press [F] to pet the cat";
    }

    public void OnInteract(PlayerRef interactor)
    {
        RPC_InteractWithCat(interactor);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_InteractWithCat(PlayerRef interactor)
    {
 
        RPC_BroadcastCatMessage("*Purr purr...*");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastCatMessage(string msg)
    {
        Debug.Log($"[Cat] {msg}");
    }
}