using TMPro;
using UnityEngine;

public class EconomyUI : MonoBehaviour
{
    public TextMeshProUGUI moneyText;

    private void OnEnable()
    {
        EconomyManager.OnRevenueUpdated += UpdateText;
    }

    private void OnDisable()
    {
        EconomyManager.OnRevenueUpdated -= UpdateText;
    }

    private void UpdateText(int currentMoney)
    {
        if (moneyText != null)
        {
            moneyText.text = $"Money: {currentMoney}";
        }
    }
}
