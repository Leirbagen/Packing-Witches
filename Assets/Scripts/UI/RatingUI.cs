using TMPro;
using UnityEngine;

public class RatingUI : MonoBehaviour
{
    [SerializeField] private TMP_Text ratingText;

    private void OnEnable()
    {
        ReputationManager.OnRatingChanged += UpdateText;
    }

    private void OnDisable()
    {
        ReputationManager.OnRatingChanged -= UpdateText;
    }

    private void UpdateText(float average)
    {
        ratingText.text = "Rating: " + average.ToString("0.0") + " / 5";
    }
}
