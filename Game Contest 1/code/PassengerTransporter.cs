using Sandbox;

public sealed class PassengerTransporter : Component, Component.ITriggerListener
{
	public List<CharacterController> Passengers { get; private set; } = new();

	private Vector3 oldPosition { get; set; }

	protected override void OnAwake()
	{
		oldPosition = Transform.Position;

	}

	public void OnTriggerEnter( Collider other )
	{

		if ( GameObject.Root.IsAncestor( other.GameObject ) ) { return; }

		if (other.GameObject.Tags.Has("player"))
		{
			Passengers.Add( other.GameObject.Components.Get<CharacterController>() );
			Log.Info( "Added passenger " + other.GameObject.Name );
		}
		
	}

	public void OnTriggerExit( Collider other )
	{
		foreach( CharacterController passenger in Passengers ) {
			if( other.GameObject == passenger.GameObject)
			{
				Passengers.Remove( passenger );
				//other.GameObject.SetParent( Scene );
				return;
			}
		}

	}

	//public void MovePassengers(Vector3 delta)
	//{
	//	base.OnFixedUpdate();

	//	if ( delta.Length <= 0f ) { return; }

	//	foreach ( CharacterController passenger in Passengers )
	//	{
	//		//TODO: PassengerContainer instead, move passengers from shipcomponent.
	//		Vector3 tVel = passenger.Velocity;

	//		passenger.Accelerate(delta / Time.Delta);
	//		passenger.Move();
	//		passenger.Velocity = tVel;

	//	}

	//}

}
