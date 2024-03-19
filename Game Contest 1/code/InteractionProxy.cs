namespace Saandy;

public class InteractionProxy : Component, IInteractable
{
	public Collider Collider { get; private set; }
	[Property] public bool BlockInteractions { get; set; } = false;
	public bool IsInteractableBy( Player player ) { return !BlockInteractions; }
	[Property] public Action<Player> OnInteracted { get; set; }

	[Property][Range(0, 10)] public float InteractionTime { get; set; } = 0f;

	[Property] public string ToolTip { get; set; }

	public string GetToolTip(Player player) { return $"{IInteractable.GetInteractionKey()} - " + ToolTip; }

	protected override void OnAwake()
	{
		base.OnAwake();

		if(Components.TryGet<Collider>( out Collider col ))
		{
			Collider = col;
		}
		else
		{
			Log.Error( "InteractionProxy does not have an attached collider." );
		}

	}

	[Broadcast]
	public void OnInteract( Guid playerId )
	{
		if ( IsProxy ) { return; }

		Player player = GameObject.Scene.Directory.FindByGuid( playerId )?.Components.Get<Player>();

		OnInteracted?.Invoke( player );
	}

}
