using TMPro;
using UnityEngine;

public class WorldPromptUI : MonoBehaviour
{
    [Header("Settings")]
    public float showDistance = 3.5f;
    public bool useFixedWorldScale = false;
    public float worldScale = 0.003f;
    public GameObject visualContainer;
    public TextMeshProUGUI promptText;

    private IInteractable[] interactables;
    private string tempMessage = "";
    private float tempMessageTimer = 0f;

    private void Awake()
    {
        interactables = GetComponentsInParent<IInteractable>();
    }

    private void Start()
    {
        if (!useFixedWorldScale)
        {
            return;
        }

        if (transform.parent != null)
        {
            Vector3 parentScale = transform.parent.lossyScale;

            float scaleX = worldScale;
            float scaleY = worldScale;
            float scaleZ = worldScale;

            if (parentScale.x != 0f)
            {
                scaleX = worldScale / parentScale.x;
            }
            if (parentScale.y != 0f)
            {
                scaleY = worldScale / parentScale.y;
            }
            if (parentScale.z != 0f)
            {
                scaleZ = worldScale / parentScale.z;
            }

            transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
        }
        else
        {
            transform.localScale = new Vector3(worldScale, worldScale, worldScale);
        }
    }

    public void ShowTemporaryMessage(string message, float duration = 2.5f)
    {
        tempMessage = message;
        tempMessageTimer = duration;
    }

    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        if (tempMessageTimer > 0f)
        {
            tempMessageTimer -= Time.deltaTime;
        }

        float distance = Vector3.Distance(transform.position, cam.transform.position);
        bool isShowingTemp = tempMessageTimer > 0f;

        if (distance <= showDistance || isShowingTemp)
        {
            string textToShow = "";

            if (isShowingTemp)
            {
                textToShow = tempMessage;
            }
            else if (interactables != null && interactables.Length > 0)
            {
                foreach (IInteractable interactable in interactables)
                {
                    string prompt = interactable.GetInteractionPrompt();
                    if (!string.IsNullOrEmpty(prompt))
                    {
                        if (!string.IsNullOrEmpty(textToShow))
                        {
                            textToShow += "\n";
                        }
                        textToShow += prompt;
                    }
                }
            }

            if (string.IsNullOrEmpty(textToShow))
            {
                visualContainer.SetActive(false);
                return;
            }

            visualContainer.SetActive(true);
            promptText.text = textToShow;
            transform.LookAt(transform.position + cam.transform.forward);
        }
        else
        {
            visualContainer.SetActive(false);
        }
    }
}