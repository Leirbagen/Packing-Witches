using System.Collections;
using UnityEngine;
using TMPro;

public class DeliveryFeedbackUI : MonoBehaviour
{
    public static DeliveryFeedbackUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject feedbackPanel;
    public TextMeshProUGUI feedbackText;
    public float defaultDuration = 3f;

    private Coroutine hideCoroutine;

    private void Awake()
    {
        Instance = this;
        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(false);
        }
    }

    public void ShowMessage(string message, float duration = -1f)
    {
        if (feedbackPanel == null || feedbackText == null)
        {
            return;
        }

        float displayTime = defaultDuration;
        if (duration > 0f)
        {
            displayTime = duration;
        }

        feedbackText.text = message;
        feedbackPanel.SetActive(true);

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        hideCoroutine = StartCoroutine(HideAfterDelay(displayTime));
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        feedbackPanel.SetActive(false);
        hideCoroutine = null;
    }
}