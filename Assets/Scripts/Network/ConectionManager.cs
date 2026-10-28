using UnityEngine;

public class ConectionManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField codeInput;

    public void Host(int sceneIndex)
    {
        BasicSpawner.Instance.HostGame(sceneIndex);
    }

    public void Join()
    {
        string code = codeInput.text.Trim().ToUpper();
        BasicSpawner.Instance.JoinGame(code);
    }
}
