using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class MachineFailureManager : NetworkBehaviour
{
    [Header("Failure Timing (Seconds)")]
    public float minTimeBetweenFailures = 25f;
    public float maxTimeBetweenFailures = 50f;
    public int maxSimultaneousBroken = 1;

    [Header("Machines")]
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
        if (!HasStateAuthority || DayManager.Instance == null)
        {
            return;
        }

        // Only trigger random breakdowns during the Active Shift
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
        List<MachineBreakdown> workingMachines = new List<MachineBreakdown>();

        foreach (MachineBreakdown machine in machines)
        {
            if (machine == null)
            {
                continue;
            }

            if (machine.IsBroken)
            {
                currentlyBroken++;
            }
            else
            {
                workingMachines.Add(machine);
            }
        }

        if (currentlyBroken >= maxSimultaneousBroken || workingMachines.Count == 0)
        {
            return;
        }

        int randomIndex = Random.Range(0, workingMachines.Count);
        workingMachines[randomIndex].BreakMachine();
    }
}