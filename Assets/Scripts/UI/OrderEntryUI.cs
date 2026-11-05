using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrderEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private Button button;
    public int OrderId { get; private set; }

    public void OrderConstructor(int id, string content)
    {
        OrderId = id;
        text.text = content;
    }
}
