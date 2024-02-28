
using Dungeon;
using Saandy;
using Sandbox.Citizen;
using Sandbox.UI;

public sealed class Player : Component
{

	public CharacterController Controller { get; set; }
	public CitizenAnimationHelper Animator { get; set; }

	[Property][Range( 0, 400, 1 )] public float WalkSpeed { get; set; } = 120f;
	[Property][Range( 0, 400, 1 )] public float RunSpeed { get; set; } = 250f;
	[Property][Range( 0, 800, 1 )] public float JumpStrength { get; set; } = 400f;

	[Category( "Camera" )] [Property] public CameraController CameraController;
	public CameraComponent Camera => CameraController?.Camera;
	[Sync] public Angles EyeAngles { get; set; }

	public EnergyBarComponent EnergyBar { get; private set; }
	public InventoryComponent Inventory { get; private set; }

	public PlayerInput PlayerInput { get; set; }

	[Sync] public CitizenAnimationHelper.HoldTypes CurrentHoldType { get; set; }

	[Category( "Bones" )][Property] public GameObject HeadBone { get; set; }
	[Category( "Bones" )][Property] public GameObject HandLBone { get; set; }
	[Category( "Bones" )][Property] public GameObject HandRBone { get; set; }
	[Category( "Bones" )][Property] public GameObject Spine1Bone { get; set; }
	[Category( "Bones" )][Property] public GameObject FlashlightRBone { get; set; }
	public Action OnJumped { get; set; }

	[Category( "Hud" )][Property] public GameObject HudObject;
	public Hud CurrentHud { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		PlayerInput = new PlayerInput( this );

		Animator = Components.Get<CitizenAnimationHelper>();
		Controller = Components.Get<CharacterController>();
		Inventory = Components.Get<InventoryComponent>();
		EnergyBar = Components.Get<EnergyBarComponent>();

		if ( GameObject.IsProxy ) {
			CameraController.Camera.Destroy();
			return; 
		}

		Animator.Target.OnFootstepEvent += OnFootstep;

		CurrentHud = HudObject.Components.Get<AliveHud>(true);
		CurrentHud.Enabled = true;

	}

	protected override void OnUpdate()
	{

		Animator.HoldType = CurrentHoldType;

		if ( GameObject.IsProxy ) { return; }

		Transform.Rotation = Rotation.FromYaw( EyeAngles.yaw );

		//

		PlayerInput?.UpdateInput();

	}

	[Broadcast]
	private void OnFootstep(SceneModel.FootstepEvent footstep)
	{
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

		if ( Controller == null )	{ return; }
		if ( Animator == null )		{ return; }

		float wantedSpeed = WalkSpeed;

		if ( PlayerInput != null && PlayerInput.WantsToRun )
		{
			if ( !EnergyBar.IsExhausted )
			{
				wantedSpeed = RunSpeed;
			}
		}

		Vector3 wantedMove = 0;
		wantedMove = (PlayerInput == null) ? 0 : PlayerInput.AnalogMove * wantedSpeed * Transform.Rotation;

		Controller.Accelerate( wantedMove );

		if ( Controller.IsOnGround )
		{
			Controller.Acceleration = 10;

			if ( Input.Pressed( "Jump" ) )
			{
				PlayerInput?.OnJump();
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

}
