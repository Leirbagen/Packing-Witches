using TMPro;
using UnityEngine;

public class ConectionManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private TMP_Dropdown regionDropdown;

    private readonly string[] regionCodes = { "", "sa", "us", "eu" };

    private string GetSelectedRegion()
    {
        if (regionDropdown != null)
        {
            return regionCodes[regionDropdown.value];
        }
        return ""; 
    }

    public void Host(int sceneIndex)
    {
        BasicSpawner.Instance.HostGame(sceneIndex, GetSelectedRegion());
    }

    public void SinglePlayer(int sceneIndex)
    {
        BasicSpawner.Instance.StartSinglePlayer(sceneIndex);
    }

    public void Join()
    {
        string code = codeInput.text.Trim().ToUpper();
        BasicSpawner.Instance.JoinGame(code, GetSelectedRegion());
    }
}
