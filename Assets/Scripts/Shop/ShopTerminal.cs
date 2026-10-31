using UnityEngine;

public class ShopTerminal : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject shopPanel;
    public static bool IsOpen { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        shopPanel.SetActive(false);
        IsOpen = false;
    }

    private void Update()
    {
        
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return !IsOpen;
    }

    public void Interact(PlayerInteractor interactor)
    {
        Open();
    }

    public void Open()
    {
        IsOpen = true;
        shopPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        IsOpen = false;
        shopPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        if (IsOpen)
        {
            Close();
        } 
    }
}
