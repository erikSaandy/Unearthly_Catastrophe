namespace Saandy;

public class InteractionProxy : Component, IInteractable
{

	[Property] public bool BlockInteractions { get; set; } = false;
	public bool IsInteractableBy( Player player ) { return !BlockInteractions; }
	public Action<Player> OnInteracted { get; set; }

	[Property][Range(0, 10)] public float InteractionTime { get; set; } = 0f;

	[Property] public string ToolTip { get; set; }

	public string GetToolTip(Player player) { return $"{IInteractable.GetInteractionKey()} - " + ToolTip; }

	public void OnInteract( Player player )
	{
		OnInteracted?.Invoke( player );
	}

}
