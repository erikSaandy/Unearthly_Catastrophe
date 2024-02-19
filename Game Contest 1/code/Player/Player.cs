
using Saandy;
using Sandbox.Citizen;

public sealed class Player : Component
{

	[Property] public CharacterController Controller { get; set; }
	[Property] public CitizenAnimationHelper Animator { get; set; }

	[Property][Range( 0, 400, 1 )] public float WalkSpeed { get; set; } = 120f;
	[Property][Range( 0, 400, 1 )] public float RunSpeed { get; set; } = 250f;
	[Property][Range( 0, 800, 1 )] public float JumpStrength { get; set; } = 400f;


	[Property] public GameObject AliveHudPrefab;
	[Category( "Camera" )] [Property] public GameObject CameraPrefab;
	public CameraComponent Camera;
	[Category( "Camera" )][Property] public Vector3 EyeOffset { get; set; }
	[Category( "Camera" )][Property][Range( 0, 90, 1 )] public float MaxPitch { get; set; } = 45;
	[Category( "Camera" )][Property][Range( -90, 0, 1 )] public float MinPitch { get; set; } = -45;

	[Sync] public Angles EyeAngles { get; set; }

	[Property] public EnergyContainer EnergyContainer { get; set; }

	public ValueBuffer<PlayerInputData> InputDataBuffer { get; private set; } = new(5);
	public PlayerInputData InputData => InputDataBuffer.Current;

	public Inventory Inventory { get; private set; }

	public Action OnJumped { get; set; }

	public Hud CurrentHud;

	protected override void DrawGizmos()
	{
		Gizmo.Draw.LineSphere( EyeOffset, 5f );
	}

	protected override void OnStart()
	{
		base.OnStart();

		InputDataBuffer.Current = new PlayerInputData();
		Inventory = new Inventory( this, 4 );

		if ( GameObject.IsProxy ) { return; }

		CameraPrefab.Clone( GameObject, Vector3.Zero, Rotation.Identity, Vector3.Zero ).Components.TryGet( out Camera );
		var hud = AliveHudPrefab.Clone();
		hud.Components.TryGet( out CurrentHud );
		CurrentHud.Owner = this;

	}

	protected override void OnUpdate()
	{

		if ( !GameObject.IsProxy ) {

			InputDataBuffer.Push();
			InputDataBuffer.Current.Update( this );

			// Update eye angles
			EyeAngles += Input.AnalogLook;
			EyeAngles = EyeAngles.WithPitch( Math.Clamp( EyeAngles.pitch, MinPitch, MaxPitch ) );

			Camera.Transform.LocalPosition = EyeOffset;
			Camera.Transform.Rotation = EyeAngles.ToRotation();
			Transform.Rotation = Rotation.FromYaw( EyeAngles.yaw );

			// Inventory scroll
			if ( Input.MouseWheel.y != 0 )
			{
				Inventory.ActiveSlot = (Inventory.ActiveSlot - Math.Sign( Input.MouseWheel.y )) % Inventory.Slots.Length;
				if ( Inventory.ActiveSlot < 0 ) { Inventory.ActiveSlot += Inventory.Slots.Length; }
			}

		}

	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		if ( GameObject.IsProxy ) { return; }

		CurrentHud?.Destroy();
	}

	protected override void OnFixedUpdate()
	{
		Animator.IsGrounded = Controller.IsOnGround;
		Animator.WithVelocity( Controller.Velocity );
		Animator.WithLook( EyeAngles.Forward, 1, .8f, .5f );
		//Animator.DuckLevel = 1f;

		base.OnFixedUpdate();

		if ( GameObject.IsProxy ) { return; }

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

	}

	void OnJump()
	{
		Controller.Punch( Vector3.Up * JumpStrength );
		Animator.TriggerJump();
		OnJumped?.Invoke();
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
