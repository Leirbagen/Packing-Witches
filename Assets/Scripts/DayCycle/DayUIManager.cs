using UnityEngine;
using TMPro;
using Fusion;

public class DayUIManager : MonoBehaviour
{
    public TextMeshProUGUI statusText;

    private void Update()
    {
        if (DayManager.Instance == null || !DayManager.Instance.Object.IsValid) return;

        DayPhase currentPhase = DayManager.Instance.CurrentPhase;

        if (currentPhase == DayPhase.Morning)
        {
            statusText.text = "PHASE: MORNING\n";
            statusText.color = Color.cyan;
        }
        else if (currentPhase == DayPhase.ActiveShift)
        {
            float? remainingTime = DayManager.Instance.ShiftTimer.RemainingTime(DayManager.Instance.Runner);
            if (remainingTime.HasValue)
            {
                int minutes = Mathf.FloorToInt(remainingTime.Value / 60f);
                int seconds = Mathf.FloorToInt(remainingTime.Value % 60f);

                statusText.text = $"ACTIVE TURN: \n{minutes:00}:{seconds:00}";
                statusText.color = remainingTime.Value < 30f ? Color.red : Color.yellow; // Se pone rojo a los 30s
            }
        }
        else if (currentPhase == DayPhase.Night)
        {
            statusText.text = "PHASE: NIGHT\n(Close)";
            statusText.color = Color.gray;
        }
    }
}