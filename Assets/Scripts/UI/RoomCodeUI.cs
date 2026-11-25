using TMPro;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class RoomCodeUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI codeText;
    private static RoomCodeUI instance;
    private bool isOnline;

    private void Awake()
    {
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
         
    }

    private void Start()
    {
        string currentCode = null;
        if (BasicSpawner.Instance != null)
        {
            currentCode = BasicSpawner.Instance.CurrentRoomCode;
        }
        isOnline = false;
        if (!string.IsNullOrEmpty(currentCode) && currentCode != "Offline")
        {
            isOnline = true;
        }
        if (isOnline)
        {
            codeText.text = $"Room Code: <color=yellow>{currentCode}</color>";
        }
        codeText.gameObject.SetActive(isOnline);
    }

    public static void SetVisible(bool show)
    {
        if (instance == null || instance.codeText == null)
        {
            return;
        }

        instance.codeText.gameObject.SetActive(show && instance.isOnline);
    }
}