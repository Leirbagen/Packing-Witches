using Fusion;
using TMPro;
using UnityEngine;

public class ShopTerminal : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TextMeshProUGUI roomCodeText;
    public static bool IsOpen { get; private set; }

    private MachineBreakdown breakdown;

    private void Awake()
    {
        breakdown = GetComponent<MachineBreakdown>();
    }

    private void Start()
    {
        shopPanel.SetActive(false);
        IsOpen = false;
    }

    private void Update()
    {
        if (IsOpen && IsMachineBroken())
        {
            Close();
        }
    }

    private bool IsMachineBroken()
    {
        return breakdown != null && breakdown.Object != null && breakdown.Object.IsValid && breakdown.IsBroken;
    }

    public string GetInteractionPrompt()
    {
        if (IsMachineBroken())
        {
            return breakdown.GetBrokenPrompt();
        }

        if (IsOpen)
        {
            return string.Empty;
        }

        return "Press [E] to open the shop";
    }

    public void OnInteract(PlayerRef interactor)
    {
        if (IsMachineBroken())
        {
            return;
        }

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
        RoomCodeUI.SetVisible(show);
    }
}