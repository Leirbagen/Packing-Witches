using Fusion;

public interface IInteractable
{
    string GetInteractionPrompt();
    void OnInteract(PlayerRef interactor);
}