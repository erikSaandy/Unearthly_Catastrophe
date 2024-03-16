using Sandbox;

public sealed class PassengerTransporter : Component, Component.ITriggerListener, Component.INetworkListener
{
	[Property] public List<Player> Passengers { get; private set; } = new();


	protected override void OnAwake()
	{
		base.OnAwake();

	}

	public void OnTriggerEnter( Collider other )
	{

		// Already added.
		if ( GameObject.Root.IsAncestor( other.GameObject ) ) { return; }


		if ( other.GameObject.Tags.Has( "player" ) )
		{
			Player player = other.GameObject.Root.Components.Get<Player>();
			if(Passengers.Contains(player)) { return; }
			if(player.LifeState == LifeState.Dead) { return; }

			Passengers.Add( player );
			player.Heal( 200 );
			Log.Info( "Added passenger " + player.GameObject.Name );

		}

	}

	public void OnTriggerExit( Collider other )
	{

		foreach ( Player passenger in Passengers )
		{
			if ( other.GameObject.Root == passenger.GameObject )
			{
				Passengers.Remove( passenger );
				Log.Info( "Removed passenger " + passenger.GameObject.Name );
				return;
			}
		}

	}

	void OnDisconnected( Connection conn )
	{
		Log.Info( "hey" );

		Player player = Passengers.First( x => x.Network.OwnerConnection == conn );
		if(player != null)
		{
			Passengers.Remove( player );
			Log.Info( "Removed passenger " + player.GameObject.Name );
		}
	}


}
