using Fusion;
using UnityEngine;
using UnityEngine.AI;

public enum CatState { Roaming, Annoying, GoingToFood, Eating }

[RequireComponent(typeof(NavMeshAgent))]
public class CatController : NetworkBehaviour, IInteractable
{
    [Header("Cat Needs")]
    public float timeUntilHungry = 120f;
    public float eatingDuration = 5f; 

    [Header("Roaming Settings")]
    public float roamRadius = 10f; 
    public float roamWaitTime = 3f; 

    [Header("Targets")]
    public Transform annoyTargetPoint;
    public Transform foodBowlTarget; 

    [Networked] public CatState CurrentState { get; set; }
    [Networked] public TickTimer HungerTimer { get; set; }
    [Networked] public TickTimer ActionTimer { get; set; } 

    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            CurrentState = CatState.Roaming;
            HungerTimer = TickTimer.CreateFromSeconds(Runner, timeUntilHungry);
            ActionTimer = TickTimer.CreateFromSeconds(Runner, roamWaitTime);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        switch (CurrentState)
        {
            case CatState.Roaming:
                if (HungerTimer.Expired(Runner))
                {
                    CurrentState = CatState.Annoying;
                    RPC_BroadcastCatMessage("<color=red>Cat is hungry!</color>");
                    if (annoyTargetPoint != null)
                    {
                        agent.SetDestination(annoyTargetPoint.position);
                    }
                    break;
                }

                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    if (ActionTimer.ExpiredOrNotRunning(Runner))
                    {
                        Vector3 randomDirection = Random.insideUnitSphere * roamRadius;
                        randomDirection += transform.position;

                        NavMeshHit hit;
                        if (NavMesh.SamplePosition(randomDirection, out hit, roamRadius, 1))
                        {
                            agent.SetDestination(hit.position);
                            ActionTimer = TickTimer.CreateFromSeconds(Runner, roamWaitTime);
                        }
                    }
                }
                break;

            case CatState.Annoying:
                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    // Lógica futura para empujar objetos
                }
                break;

            case CatState.GoingToFood:
                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    CurrentState = CatState.Eating;
                    ActionTimer = TickTimer.CreateFromSeconds(Runner, eatingDuration); // Empieza a comer
                    RPC_BroadcastCatMessage("*Nom nom nom...*");
                }
                break;

            case CatState.Eating:
                if (ActionTimer.Expired(Runner))
                {
                    CurrentState = CatState.Roaming;
                    HungerTimer = TickTimer.CreateFromSeconds(Runner, timeUntilHungry); // Se reinicia su hambre
                    RPC_BroadcastCatMessage("<color=green>Cat is full and happy.</color>");
                }
                break;
        }
    }

    public void FeedCat()
    {
        if (!HasStateAuthority) return;

        if (foodBowlTarget != null)
        {
            CurrentState = CatState.GoingToFood;
            agent.SetDestination(foodBowlTarget.position);
            RPC_BroadcastCatMessage("<color=yellow>Cat noticed the food and is coming!</color>");
        }
    }

    public string GetInteractionPrompt()
    {
        if (CurrentState == CatState.Annoying) return "Cat is angry! (Go fill the food bowl)";
        if (CurrentState == CatState.GoingToFood) return "Cat is running to eat";
        if (CurrentState == CatState.Eating) return "Cat is eating... do not disturb";

        float time = HungerTimer.RemainingTime(Runner) ?? 0f;
        return $"Press [F] to pet the cat (Hunger: {time:F0}s)";
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