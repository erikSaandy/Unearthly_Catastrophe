using Sandbox;

public sealed class PassengerTransporter : Component, Component.ITriggerListener
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
			Passengers.Add( player );
			Log.Info( "Added passenger " + other.GameObject.Name );

		}

	}

	public void OnTriggerExit( Collider other )
	{

		foreach ( Player passenger in Passengers )
		{
			if ( other.GameObject == passenger.GameObject )
			{
				Passengers.Remove( passenger );
				Log.Info( "Removed passenger " + other.GameObject.Name );
				return;
			}
		}

	}


}
