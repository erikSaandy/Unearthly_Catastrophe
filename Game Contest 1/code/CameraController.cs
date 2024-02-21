using Sandbox;

public sealed class CameraController : Component
{
	[Property] public Player Owner { get; private set; }
	[Property] public Vector3 EyeOffset { get; set; }
	[Property][Range( 0, 90, 1 )] public float MaxPitch { get; set; } = 75;
	[Property][Range( -90, 0, 1 )] public float MinPitch { get; set; } = -45;

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

		Owner.EyeAngles += Input.AnalogLook;
		Owner.EyeAngles = Owner.EyeAngles.WithPitch( Math.Clamp( Owner.EyeAngles.pitch, MinPitch, MaxPitch ) );

		//Transform.LocalPosition = EyeOffset;

		Transform eyeTx = Owner.Animator.Target.GetAttachment( "eyes" ) ?? default;
		Transform.Position = eyeTx.Position;
		Transform.Rotation = Owner.EyeAngles.ToRotation();

	}

}
