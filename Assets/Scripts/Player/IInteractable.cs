using Fusion;

public interface IInteractable
{
    string GetInteractionPrompt();
    public void OnInteract(PlayerRef interactor);
}