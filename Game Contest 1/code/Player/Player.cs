
using Sandbox.Citizen;
using Sandbox.UI;
using static System.Runtime.InteropServices.JavaScript.JSType;

public sealed class Player : Component
{
	[Property] public GameObject Camera { get; set; }
	[Property] public CharacterController Controller { get; set; }
	[Property] public CitizenAnimationHelper Animator { get; set; }

	[Property][Range( 0, 400, 1 )] public float WalkSpeed { get; set; } = 120f;
	[Property][Range( 0, 400, 1 )] public float RunSpeed { get; set; } = 250f;
	[Property][Range( 0, 800, 1 )] public float JumpStrength { get; set; } = 400f;

	[Category( "Camera" )][Property][Range( 0, 90, 1 )] public float MaxPitch { get; set; } = 45;
	[Category( "Camera" )][Property][Range( -90, 0, 1 )] public float MinPitch { get; set; } = -45;

	public Angles EyeAngles { get; set; }

	[Property] public EnergyContainer EnergyContainer;

	public ValueBuffer<PlayerInputData> InputDataBuffer { get; private set; } = new(5);
	public PlayerInputData InputData => InputDataBuffer.Current;

	protected override void OnAwake()
	{
		InputDataBuffer.Current = new PlayerInputData();

		base.OnAwake();
	}

	protected override void OnStart()
	{
		base.OnStart();
	}

	protected override void OnUpdate()
	{
		InputDataBuffer.Push();
		InputDataBuffer.Current.Update( this );

		EnergyContainer?.Update();

		// Update eye angles
		EyeAngles += Input.AnalogLook;
		EyeAngles = EyeAngles.WithPitch( Math.Clamp( EyeAngles.pitch, MinPitch, MaxPitch ) );

		Transform.Rotation = Rotation.FromYaw( EyeAngles.yaw );
		Camera.Transform.Rotation = EyeAngles.ToRotation();

	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( Controller == null ) { return; }
		if(Animator == null) { return; }

		float wantedSpeed = WalkSpeed;
		Vector3 wantedMove = 0;
		if (InputData.WantsToRun)
		{
			if(!EnergyContainer.IsExhausted)
			{
				wantedSpeed = RunSpeed;
			}
		}

		wantedMove = InputDataBuffer.Current.AnalogMove * wantedSpeed * Transform.Rotation;
		Controller.Accelerate( wantedMove );

		if( InputDataBuffer.Current.IsGrounded)
		{
			Controller.Acceleration = 10;

			if(Input.Pressed("Jump"))
			{
				OnJump();
			}
			else
			{
				Controller.ApplyFriction( 5f );
			}
		}
		else
		{
			Controller.Acceleration = 5;
			Controller.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;
		}

		Controller.Move();

		Animator.IsGrounded = InputData.IsGrounded;
		Animator.WithVelocity( Controller.Velocity );
		Animator.WithLook( EyeAngles.Forward, 1, .8f, .5f );
		//Animator.DuckLevel = 1f;
	}

	void OnJump()
	{
		Controller.Punch( Vector3.Up * JumpStrength );

		Animator.TriggerJump();

	}



	public class PlayerInputData
	{
		public Player Owner { get; private set; }

		public Vector3 AnalogMove { get; set; } = Vector3.Zero;
		public bool WantsToRun = false;
		public bool IsGrounded = false;

		public bool HasInput => Owner.InputDataBuffer.Current.AnalogMove.Length > 0f;
		public bool IsMoving => Owner.Controller.Velocity.WithY( 0 ).Length > 5f;

		public void Update(Player player)
		{
			Owner = player;
			AnalogMove = Input.AnalogMove.Normal;
			WantsToRun = Input.Down( "Run" );
			IsGrounded = player.Controller.IsOnGround;

		}

	}

}
