
using Dungeon;
using Saandy;
using Sandbox.Citizen;
using Sandbox.UI;
using static Sandbox.Gizmo;

public sealed class Player : Component, Component.INetworkListener, IKillable
{

	[Sync] public LifeState LifeState { get; private set; } = LifeState.Alive;
	[Sync, Property] public float MaxHealth { get; private set; } = 100f;
	[Sync] public float Health { get; private set; } = 100f;
	private RealTimeSince TimeSinceDamaged { get; set; }
	public RealTimeSince TimeSinceDeath { get; private set; }

	public CharacterController Controller { get; set; }
	public SkinnedModelRenderer Renderer { get; private set; }
	public CitizenAnimationHelper Animator { get; set; }
	public Voice Voice { get; set; }

	[Property][Range( 0, 400, 1 )] public float WalkSpeed { get; set; } = 120f;
	[Property][Range( 0, 400, 1 )] public float RunSpeed { get; set; } = 250f;
	[Property][Range( 0, 800, 1 )] public float JumpStrength { get; set; } = 400f;

	[Category( "Camera" )] [Property] public CameraController CameraController;
	public CameraComponent Camera => CameraController?.Camera;

	[Sync] public Angles EyeAngles { get; set; }

	public EnergyBarComponent EnergyBar { get; private set; }
	public InventoryComponent Inventory { get; private set; }
	public RagdollController Ragdoll { get; private set; }

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

	public GameObject OldGroundObject { get; private set; } = null;
	public GameObject LastGroundObject { get; private set; } = null;

	protected override void OnStart()
	{
		base.OnStart();

		GameObject.BreakFromPrefab();

		Animator = Components.Get<CitizenAnimationHelper>();
		Controller = Components.Get<CharacterController>();
		Inventory = Components.Get<InventoryComponent>();
		EnergyBar = Components.Get<EnergyBarComponent>();
		Ragdoll = Components.Get<RagdollController>( true );
		Voice = Components.Get<Voice>();
		Renderer = Components.Get<SkinnedModelRenderer>();

		PlayerInput = new PlayerInput( this );

		if ( GameObject.IsProxy ) {

			HideHead( false );
			HudObject.Destroy();
			CameraController.Camera.Destroy();
			return; 
		}

		LethalGameManager.OnPlayerConnected( GameObject.Id );

		LethalGameManager.OnStartLoadMoon += OnStartLoadMoon;
		LethalGameManager.OnLoadedMoon += OnLoadedMoon;

		CurrentHud = HudObject.Components.Get<AliveHud>(true);
		CurrentHud.Enabled = true;

		HideHead( true );
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();
	}


	public void HideHead(bool hide = true)
	{
		// yeah I know lmao
		List<GameObject> hairObjects = GameObject.Children.FindAll( x => x.Name.Contains( "hair" ) || x.Name.Contains( "eyebrows" ) || x.Name.Contains( "stubble" ) || x.Name.Contains("eyelashes") );

		foreach ( GameObject hair in hairObjects )
		{
			hair.Enabled = !hide;
		}

		if ( hide )
		{
			Renderer.SetBodyGroup( "head", 1 );
		}
		else
		{
			Renderer.SetBodyGroup( "head", 0 );
		}

	}

	protected override void OnUpdate()
	{
		PlayerInput?.UpdateInput();
		PlayerInput?.CameraInput();

		if ( Ragdoll.IsRagdolled || LifeState == LifeState.Dead )
			return;

		Animator.IsGrounded = Controller.IsOnGround;
		Animator.WithVelocity( Controller.Velocity );
		Animator.WithLook( EyeAngles.Forward, 1, .8f, .5f );
		//Animator.DuckLevel = 1f;
		Animator.HoldType = CurrentHoldType;

	}


	[Broadcast]
	public void TakeDamage( float damage, Guid attackerId, Vector3 impulseForce = default )
	{
		if ( LifeState == LifeState.Dead )
			return;

		if ( IsProxy )
			return;

		TimeSinceDamaged = 0f;
		Health = MathF.Max( Health - damage, 0f );

		Log.Info( impulseForce );
		Controller.Punch( impulseForce * 10000 );

		if ( Health <= 0f )
		{
			LifeState = LifeState.Dead;
			Ragdoll.Ragdoll();
			Inventory.DropAll();
			OnKilled( Scene.Directory.FindByGuid( attackerId ) );
			TimeSinceDeath = 0;
		}
	}

	private void OnKilled( GameObject attacker )
	{
		/*
		if ( attacker.IsValid() )
		{
			var player = attacker.Components.GetInAncestorsOrSelf<Player>();
			if ( player.IsValid() )
			{
				var chat = Scene.GetAllComponents<Chat>().FirstOrDefault();

				if ( chat.IsValid() )
					chat.AddTextLocal( "💀️", $"{player.Network.OwnerConnection.DisplayName} has killed {Network.OwnerConnection.DisplayName}" );
			}
		}
		*/

		Tags.Add( "dead" );

		if ( IsProxy )
			return;

		HideHead( false );
		PlayerInput = new PlayerSpectateInput( this );

		LethalGameManager.Instance?.OnPlayerDeath( GameObject.Id );

	}

	public void Kill()
	{
		if ( LifeState == LifeState.Dead ) { return; }

		TakeDamage( Health + 100, GameObject.Id );
	}

	[Broadcast]
	public void Respawn()
	{
		Tags.Remove( "dead" );

		if ( IsProxy ) { return; }

		Controller.Velocity = 0;
		Controller.Acceleration = 0;
		Ragdoll.Unragdoll(); 
		MoveToSpawnPoint();
		LifeState = LifeState.Alive;
		Health = MaxHealth;
		PlayerInput = new PlayerInput( this );
		HideHead( true );

	}

	private void MoveToSpawnPoint()
	{
		var spawnpoints = Scene.GetAllComponents<SpawnPoint>();
		var randomSpawnpoint = LethalGameManager.Random.FromList( spawnpoints.ToList() );
		TeleportTo( randomSpawnpoint.Transform.Position, randomSpawnpoint.Transform.Rotation );
	}

	//

	public void TeleportTo( Vector3 position ) { TeleportTo( position, Transform.Rotation ); }

	[Broadcast]
	public void TeleportTo( Vector3 position, Rotation rotation )
	{
		if ( IsProxy ) { return; };

		Transform.Position = position;
		Transform.Rotation = rotation;
		EyeAngles = Transform.Rotation;

	}
	
	//

	public void OnStartLoadMoon()
	{
		if ( IsProxy ) { return; }

		PlayerInput = new PlayerFreezeInput( this );
	}

	public void OnLoadedMoon()
	{
		if ( IsProxy ) { return; }

		PlayerInput = new PlayerInput( this );
	}

	protected override void OnDestroy()
	{

		base.OnDestroy();

		if ( GameObject.IsProxy ) { return; }

		LethalGameManager.OnPlayerDisconnected( GameObject.Id );

		CurrentHud?.Destroy();

		LethalGameManager.OnStartLoadMoon -= OnStartLoadMoon;
		LethalGameManager.OnLoadedMoon -= OnLoadedMoon;

	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( Controller == null ) { return; }
		if ( Animator == null ) { return; }

		if ( GameObject.IsProxy ) { return; }

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

		if ( Controller.GroundObject != OldGroundObject )
		{
			OnGroundChanged( OldGroundObject, Controller.GroundObject );
			OldGroundObject = Controller.GroundObject;
		}

		PlayerInput?.FixedUpdateInput();

		Controller.Move();

		Transform.Rotation = Rotation.FromYaw( EyeAngles.ToRotation().Yaw() );

	}


	private void OnGroundChanged(GameObject oldGround, GameObject newGround)
	{
		if ( newGround != null )
		{
			LastGroundObject = newGround;
		}

		if(LastGroundObject == LethalGameManager.Instance.Ship.GameObject )
		{
			//GameObject.SetParent( newGround );
		}
		else
		{
			//GameObject.SetParent( Scene );
		}


	}

}
