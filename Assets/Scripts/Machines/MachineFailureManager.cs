using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class MachineFailureManager : NetworkBehaviour
{
    public float minTimeBetweenFailures = 25f;
    public float maxTimeBetweenFailures = 50f;
    public int maxSimultaneousBroken = 1;

    public List<MachineBreakdown> machines = new List<MachineBreakdown>();

    [Networked]
    private TickTimer nextFailureTimer { get; set; }

    public override void Spawned()
    {
        if (!HasStateAuthority)
        {
            return;
        }

        if (machines.Count == 0)
        {
            MachineBreakdown[] found = FindObjectsByType<MachineBreakdown>(FindObjectsSortMode.None);
            foreach (MachineBreakdown machine in found)
            {
                machines.Add(machine);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || DayManager.Instance == null || !DayManager.Instance.Object.IsValid)
        {
            return;
        }

        if (DayManager.Instance.CurrentPhase != DayPhase.ActiveShift)
        {
            nextFailureTimer = TickTimer.None;
            return;
        }

        if (!nextFailureTimer.IsRunning)
        {
            ScheduleNextFailure();
            return;
        }

        if (nextFailureTimer.Expired(Runner))
        {
            TryBreakRandomMachine();
            ScheduleNextFailure();
        }
    }

    private void ScheduleNextFailure()
    {
        float delay = Random.Range(minTimeBetweenFailures, maxTimeBetweenFailures);
        nextFailureTimer = TickTimer.CreateFromSeconds(Runner, delay);
    }

    private void TryBreakRandomMachine()
    {
        int currentlyBroken = 0;
        List<MachineBreakdown> breakableMachines = new List<MachineBreakdown>();

        foreach (MachineBreakdown machine in machines)
        {
            if (machine == null || machine.Object == null || !machine.Object.IsValid)
            {
                continue;
            }

            if (machine.IsBroken)
            {
                currentlyBroken++;
            }
            else if (machine.GetFailureChance() > 0f)
            {
                breakableMachines.Add(machine);
            }
        }

        if (currentlyBroken >= maxSimultaneousBroken || breakableMachines.Count == 0)
        {
            return;
        }

        int randomIndex = Random.Range(0, breakableMachines.Count);
        MachineBreakdown selected = breakableMachines[randomIndex];

        if (Random.value <= selected.GetFailureChance())
        {
            selected.BreakMachine();
        }
    }
}