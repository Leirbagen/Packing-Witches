using UnityEngine;
using TMPro;

public class RoomCodeUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI codeText;

    private void Start()
    {
        if (BasicSpawner.Instance != null)
        {
            string currentCode = BasicSpawner.Instance.CurrentRoomCode;

            if (currentCode == "Offline" || string.IsNullOrEmpty(currentCode))
            {
                gameObject.SetActive(false);
            }
            else
            {
                codeText.text = $"Room Code: <color=yellow>{currentCode}</color>";
            }
        }
    }
}