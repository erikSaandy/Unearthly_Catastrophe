
using Dungeon;
using Saandy;
using Sandbox.Citizen;

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

	public ValueBuffer<PlayerInputData> InputDataBuffer { get; private set; } = new(5);
	public PlayerInputData InputData => InputDataBuffer.Current;

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

		Animator = Components.Get<CitizenAnimationHelper>();
		Controller = Components.Get<CharacterController>();
		Inventory = Components.Get<InventoryComponent>();
		EnergyBar = Components.Get<EnergyBarComponent>();

		if ( GameObject.IsProxy ) {
			CameraController.Camera.Destroy();
			return; 
		}

		Animator.Target.OnFootstepEvent += OnFootstep;

		InputDataBuffer.Current = new PlayerInputData();

		CurrentHud = HudObject.Components.Get<AliveHud>(true);
		CurrentHud.Enabled = true;

	}

	protected override void OnUpdate()
	{
		Animator.HoldType = CurrentHoldType;

		if ( GameObject.IsProxy ) { return; }

		InputDataBuffer.Push();
		InputDataBuffer.Current.Update( this );

		Transform.Rotation = Rotation.FromYaw( EyeAngles.yaw );

		//

		if ( Input.Pressed( "use" ) )
		{
			var from = CameraController.Camera.Transform.Position;
			var to = from + Camera.Transform.Rotation.Forward * 70;
			IEnumerable<SceneTraceResult> trace = Scene.Trace.Ray( from, to ).IgnoreGameObjectHierarchy( GameObject ).WithoutTags( "owned" ).UseRenderMeshes().Size( 12f ).RunAll();

			IInteractable interactable = null;
			foreach ( SceneTraceResult item in trace )
			{
				item.GameObject.Components.TryGet( out interactable );
				if ( interactable != null )
				{
					interactable.OnInteract(this);
					break;
				}
			}

		}

		if ( Input.Pressed( "attack1" ) )
		{
			Inventory?.ActiveItem?.OnUsePrimary();
		}

		if ( Input.Pressed( "attack2" ) )
		{
			Inventory?.ActiveItem?.OnUseSecondary();
		}

		if ( Input.Pressed( "drop" ) )
		{
			Inventory?.DropActive();
		}

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

		if ( Controller == null ) { return; }
		if( Animator == null) { return; }

		float wantedSpeed = WalkSpeed;
		Vector3 wantedMove = 0;
		if (InputData.WantsToRun)
		{
			if(!EnergyBar.IsExhausted)
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

		public bool IsRunning => WantsToRun && HasInput && IsMoving;

		public void Update(Player player)
		{
			Owner = player;
			AnalogMove = Input.AnalogMove.Normal;
			WantsToRun = Input.Down( "Run" );
			IsGrounded = player.Controller.IsOnGround;

		}

	}

}
