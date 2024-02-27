using Sandbox;

namespace Saandy;

public sealed class InteractionProxy : Component, IInteractable
{
	public Action<Player> OnInteracted { get; set; }

	public void OnInteract( Player player )
	{
		OnInteracted?.Invoke( player );
	}
}
