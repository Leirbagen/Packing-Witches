using Fusion;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI; // NUEVO: Necesario para usar Sliders

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

    [Header("UI")]
    public Slider hungerSlider;       
    public Transform canvasTransform;
    public WorldPromptUI worldPrompt;
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
                    if (annoyTargetPoint != null)
                    {
                        agent.SetDestination(annoyTargetPoint.position);
                    }
                    RPC_BroadcastCatMessage("<color=#FF4444>Cat is hungry!</color>");
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
                if (!agent.pathPending && agent.remainingDistance < 0.5f) { /* Empujar objetos */ }
                break;

            case CatState.GoingToFood:
                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    CurrentState = CatState.Eating;
                    ActionTimer = TickTimer.CreateFromSeconds(Runner, eatingDuration);
                    RPC_BroadcastCatMessage("*Nom nom nom...*");
                }
                break;

            case CatState.Eating:
                if (ActionTimer.Expired(Runner))
                {
                    CurrentState = CatState.Roaming;
                    HungerTimer = TickTimer.CreateFromSeconds(Runner, timeUntilHungry);
                    RPC_BroadcastCatMessage("<color=#44FF44>Cat is full and happy.</color>");
                }
                break;
        }
    }

    private void LateUpdate()
    {
        if (Runner != null && Runner.IsRunning && hungerSlider != null)
        {
            float remaining = HungerTimer.RemainingTime(Runner) ?? 0f;

            if (CurrentState == CatState.Eating || CurrentState == CatState.GoingToFood)
            {
                hungerSlider.value = 1f;
            }
            else
            {
                hungerSlider.value = remaining / timeUntilHungry;
            }
        }

        if (canvasTransform != null && Camera.main != null)
        {
            canvasTransform.LookAt(canvasTransform.position + Camera.main.transform.forward);
        }
    }

    public void FeedCat()
    {
        if (!HasStateAuthority) return;

        if (foodBowlTarget != null)
        {
            CurrentState = CatState.GoingToFood;
            agent.SetDestination(foodBowlTarget.position);
            RPC_BroadcastCatMessage("<color=#FFFF00>Cat noticed the food and is coming!</color>");
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

    public void OnInteract(PlayerRef interactor) { RPC_InteractWithCat(interactor); }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_InteractWithCat(PlayerRef interactor) 
    {
        RPC_BroadcastCatMessage("*Purr purr...*"); 
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastCatMessage(string msg)
    {
        worldPrompt.ShowTemporaryMessage(msg);
    }
}