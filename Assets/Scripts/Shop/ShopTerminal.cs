using Fusion;
using TMPro;
using UnityEngine;

public class ShopTerminal : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TextMeshProUGUI roomCodeText;
    public static bool IsOpen { get; private set; }

    private void Start()
    {
        shopPanel.SetActive(false);
        IsOpen = false;
    }

    public string GetInteractionPrompt()
    {
        if (IsOpen)
        {
            return string.Empty;
        }
        return "Press [E] to open the shop";
    }

    public void OnInteract(PlayerRef interactor)
    {
        if (IsOpen)
        {
            return;
        }
        Open();
    }

    public void Open()
    {
        IsOpen = true;
        shopPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ToggleRoomCode(false);
    }

    public void Close()
    {
        IsOpen = false;
        shopPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ToggleRoomCode(true);
    }

    private void OnDisable()
    {
        if (IsOpen)
        {
            IsOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void ToggleRoomCode(bool show)
    {
        /*
        GameObject roomCodeObj = GameObject.Find("RoomCodeText");

        if (roomCodeObj != null)
        {
            var textComp = roomCodeObj.GetComponent<TMPro.TextMeshProUGUI>();
            if (textComp != null)
            {
                textComp.enabled = show;
            }
        }
        */
        RoomCodeUI.SetVisible(show);
    }
}