using Sandbox;

public sealed class PassengerTransporter : Component, Component.ITriggerListener
{
	private List<CharacterController> Passengers { get; set; } = new();

	private Vector3 oldPosition { get; set; }

	protected override void OnAwake()
	{
		oldPosition = Transform.Position;
	}

	public void OnTriggerEnter( Collider other )
	{

		if ( other.GameObject.IsAncestor( GameObject.Root ) ) { return; }

		if (other.GameObject.Tags.Has("player"))
		{
			Passengers.Add( other.GameObject.Components.Get<CharacterController>() );
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

	public void MovePassengers(Vector3 delta)
	{
		base.OnFixedUpdate();

		if ( delta.Length <= 0f ) { return; }

		foreach ( CharacterController passenger in Passengers )
		{
			Vector3 tVel = passenger.Velocity;

			passenger.Velocity = delta / Time.Delta;
			passenger.Move();
			passenger.Velocity = tVel;

		}

	}

}
