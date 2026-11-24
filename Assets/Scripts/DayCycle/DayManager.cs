using Fusion;
using UnityEngine;

public enum DayPhase
{
    Morning,
    ActiveShift,
    Night
}

public class DayManager : NetworkBehaviour
{
    public static DayManager Instance { get; private set; }

    [Header("Day Configuration")]
    public float shiftDurationInSeconds = 300f;
    [Networked] public DayPhase CurrentPhase { get; set; }
    [Networked] public TickTimer ShiftTimer { get; set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (HasStateAuthority && Input.GetKeyDown(KeyCode.C))
        {
            if (CurrentPhase == DayPhase.Morning)
            {
                UI_StartActiveShift();
            }
        }
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            ChangePhase(DayPhase.Morning);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
        {
            return;
        }

        if (CurrentPhase == DayPhase.ActiveShift)
        {
            if (ShiftTimer.Expired(Runner))
            {
                ChangePhase(DayPhase.Night);
            }
        }
    }

    public void ChangePhase(DayPhase newPhase)
    {
        if (!HasStateAuthority)
        {
            return;
        }

        CurrentPhase = newPhase;

        if (CurrentPhase == DayPhase.ActiveShift)
        {
            ShiftTimer = TickTimer.CreateFromSeconds(Runner, shiftDurationInSeconds);
        }
        else
        {
            ShiftTimer = TickTimer.None;
        }
    }

    public void UI_StartActiveShift()
    {
        ChangePhase(DayPhase.ActiveShift);
    }

    public void UI_ChangePhaseByIndex(int phaseIndex)
    {
        ChangePhase((DayPhase)phaseIndex);
    }
}