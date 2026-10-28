using UnityEngine;
using Rewired;
using System.Collections.Generic;

public enum InputMode
{
    Gameplay,
    Menu
}

public class InputController : MonoBehaviour
{
    public int playerId = 0;
    public static InputController Instance { get; private set; }

    [SerializeField] private InputMode startingMode = InputMode.Menu;
    private InputMode currentGameMode;
    private Player player;

    static Dictionary<InputMode, string> gameModeToMapCategory = new Dictionary<InputMode, string>() {
        { InputMode.Gameplay, "inGame" },
        { InputMode.Menu, "inMenu" },
    };

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        player = ReInput.players.GetPlayer(playerId);
        player.controllers.ControllerAddedEvent += OnControllerAdded;

        ChangeGameMode(startingMode);
    }

    void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        if (ReInput.isReady && player != null)
        {
            player.controllers.ControllerAddedEvent -= OnControllerAdded;
        }
    }

    public void ChangeGameMode(InputMode mode)
    {
        currentGameMode = mode;
        SetControllerMapsForCurrentGameMode();
    }

    void OnControllerAdded(ControllerAssignmentChangedEventArgs args)
    {
        SetControllerMapsForCurrentGameMode();
    }

    void SetControllerMapsForCurrentGameMode()
    {
        player.controllers.maps.SetAllMapsEnabled(false);
        player.controllers.maps.SetMapsEnabled(true, gameModeToMapCategory[currentGameMode]);
    }

    public float GetAxis(int actionID)
    {
        return player.GetAxis(actionID);
    }

    public bool GetButtonDown(int actionID)
    {
        return player.GetButtonDown(actionID);
    }

    public bool GetButton(int actionID)
    {
        return player.GetButton(actionID);
    }

    public bool GetMouseButton(int actionID)
    {
        return player.GetButtonDown(actionID);
    }

    public static class Input
    {
        public const int MOVEMENT_Y = 0;
        public const int MOVEMENT_X = 1;
        public const int MOUSE_X = 2;
        //public const int MOUSE_Y = 4;
        //public const int JUMP = 2;
    }
}