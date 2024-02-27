using Saandy;
using Sandbox;
using System.Numerics;

public sealed class CameraController : Component
{
	[Property] public Player Owner { get; private set; }
	[Property] public Vector3 EyeOffset { get; set; }
	[Property][Range( 0, 90, 1 )] public float MaxPitch { get; private set; } = 75;
	[Property][Range( -90, 0, 1 )] public float MinPitch { get; private set; } = -45;

	public CameraComponent Camera { get; set; }

	protected override void OnAwake()
	{
		base.OnStart();

		Camera = GameObject.Components.Get<CameraComponent>();

	}

	protected override void OnUpdate()
	{
		if ( GameObject.IsProxy ) { return; }

		// Update eye angles

		Owner.PlayerInput?.CameraInput();

	}

}
