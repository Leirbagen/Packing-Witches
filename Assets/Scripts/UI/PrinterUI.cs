
using TMPro;
using UnityEngine;

public class PrinterUI : MonoBehaviour
{
    [SerializeField] private StampPrinter printer;
    [SerializeField] private TMP_InputField contentInput;

    public void Print()
    {
        if (printer == null || contentInput == null)
        {
            return;
        }
        string content = contentInput.text.Trim();

        if (string.IsNullOrWhiteSpace(content))
        {
            Debug.LogWarning("[PrinterUI] Write a destination first.");
            return;
        }
        printer.RequestPrint(content);
    }
}
