
using Dungeon;
using Saandy;
using Sandbox.Citizen;
using Sandbox.UI;
using static Sandbox.Gizmo;

public sealed class Player : Component, IKillable
{

	[Sync, Property] public LifeState LifeState { get; private set; } = LifeState.Alive;
	[Sync, Property] public float MaxHealth { get; private set; } = 100f;
	[Sync, Property] public float Health { get; private set; } = 100f;
	private RealTimeSince TimeSinceDamaged { get; set; }
	public RealTimeSince TimeSinceDeath { get; private set; }

	public CharacterController Controller { get; set; }
	public Vector3 WishVelocity { get; set; }

	public SkinnedModelRenderer Renderer { get; private set; }
	public CitizenAnimationHelper Animator { get; set; }
	public Voice Voice { get; set; }

	[Property][Range( 0, 400, 1 )] public float WalkSpeed { get; set; } = 120f;
	[Property][Range( 0, 400, 1 )] public float RunSpeed { get; set; } = 250f;
	[Property][Range( 0, 800, 1 )] public float JumpStrength { get; set; } = 400f;

	[Category( "Camera" )] [Property] public CameraController CameraController;
	public CameraComponent Camera => CameraController?.Camera;

	[Category( "Components" )][Property] public CapsuleCollider PhysicsCollider { get; private set; }

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

	[Category( "Sounds" )][Property] public SoundEvent HurtSound { get; set; }

	public Action OnJumped { get; set; }

	[Category( "Hud" )][Property] public GameObject HudObject;
	public Hud CurrentHud { get; set; }

	public GameObject OldGroundObject { get; private set; } = null;
	public GameObject LastGroundObject { get; private set; } = null;
	public RealTimeSince TimeSinceGrounded { get; set; } = 0;

	protected override void OnStart()
	{
		base.OnStart();

		if(GameObject.IsPrefabInstance)
		{
			//GameObject.BreakFromPrefab();
		}

		Animator = Components.Get<CitizenAnimationHelper>();
		Controller = Components.Get<CharacterController>();
		Inventory = Components.Get<InventoryComponent>();
		EnergyBar = Components.Get<EnergyBarComponent>();
		Ragdoll = Components.Get<RagdollController>( true );
		Renderer = Components.Get<SkinnedModelRenderer>();

		PlayerInput = new PlayerInput( this );

		if ( GameObject.IsProxy ) {

			HideHead( false );
			HudObject.Enabled = false;
			if(Camera != null)
			{
				CameraController.Camera.GameObject.Enabled = false;
			}
			return; 
		}

		Voice = Components.Get<Voice>();
		Voice.Mode = Voice.ActivateMode.PushToTalk;

		LethalGameManager.OnPlayerConnected( GameObject.Id );

		Log.Info( GameObject.Name + "start" );
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
			//Renderer.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
			Renderer.SetBodyGroup( "head", 1 );
		}
		else
		{
			Renderer.SetBodyGroup( "head", 0 );
		}

	}

	[Broadcast]
	public void Heal( float amount )
	{
		if ( IsProxy || LifeState == LifeState.Dead )
			return;

		Health = Math.Clamp( Health + amount, 0, MaxHealth );

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
		Controller.Velocity *= 0.5f;

		if ( HurtSound is not null )
		{
			Sound.Play( HurtSound, Transform.Position );
		}

		if ( Health <= 0f )
		{
			LifeState = LifeState.Dead;
			Ragdoll.Ragdoll();
			Inventory.DropAll();
			OnKilled( attackerId );
			TimeSinceDeath = 0;
		}
	}

	[Broadcast]
	private void OnKilled( Guid attackerId )
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

		LifeState = LifeState.Dead;
		Tags.Add( "dead" );

		PhysicsCollider.Enabled = false;
		PlayerInput = new PlayerSpectateInput( this );

		if ( IsProxy )
			return;

		HideHead( false );

		LethalGameManager.Instance?.QueueOnPlayerDeath();

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
		PhysicsCollider.Enabled = true;

		if ( IsProxy ) { return; }

		PlayerInput = new PlayerInput( this );
		LifeState = LifeState.Alive;
		Controller.Velocity = 0;
		Ragdoll.Unragdoll(); 
		MoveToSpawnPoint();
		Health = MaxHealth;
		HideHead( true );
		TimeSinceGrounded = 0;

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

		LifeState = LifeState.Dead;
		LethalGameManager.OnPlayerDisconnected( GameObject.Id );

		CurrentHud?.Destroy();

		LethalGameManager.OnStartLoadMoon -= OnStartLoadMoon;
		LethalGameManager.OnLoadedMoon -= OnLoadedMoon;

	}

	protected override void OnUpdate()
	{

		PlayerInput?.CameraInput();

		if ( Ragdoll.IsRagdolled || LifeState == LifeState.Dead )
			return;

		Animator.IsGrounded = Controller.IsOnGround;
		Animator.WithVelocity( Controller.Velocity );
		Animator.WithWishVelocity( WishVelocity );
		Animator.WithLook( EyeAngles.Forward, 1, .8f, .5f );
		Animator.HoldType = CurrentHoldType;
		Animator.FootShuffle = 0f;
		//Animator.DuckLevel = 1f;

		if ( IsProxy ) { return; }

		if ( TimeSinceGrounded > 10f )
		{
			Kill();
			Log.Info( $"{GameObject.Name} killed since they had been falling for a long time." );
		}

	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		PlayerInput?.UpdateInput();

		if ( Ragdoll.IsRagdolled || LifeState == LifeState.Dead )
			return;

		//if ( Controller == null ) { return; }
		//if ( Animator == null ) { return; }

		//Vector3 wantedMove = 0;
		//wantedMove = (PlayerInput == null) ? 0 : PlayerInput.AnalogMove * wantedSpeed * Transform.Rotation;

		if ( Controller.IsOnGround )
		{
			TimeSinceGrounded = 0;
			Controller.Accelerate( WishVelocity );
			Controller.ApplyFriction( 4.0f );
			Controller.Acceleration = 10;

			if ( Input.Pressed( "Jump" ) )
			{
				PlayerInput?.OnJump();
			}
		}
		else
		{
			Controller.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;
			Controller.Accelerate( WishVelocity.ClampLength( 50 ) );
			Controller.ApplyFriction( 0.1f );
			Controller.Acceleration = 5;
		}

		if ( !IsProxy )
		{
			Controller.Move();
		}

		if ( Controller.IsOnGround )
		{
			Controller.Velocity = Controller.Velocity.WithZ( 0 );
			//LastUngroundedTime = 0f;
		}

		if ( IsProxy ) { return; }


		if ( Controller.GroundObject != OldGroundObject )
		{
			OnGroundChanged( OldGroundObject, Controller.GroundObject );
			OldGroundObject = Controller.GroundObject;
		}

		//if(Input.EscapePressed)
		//{
		//	Game.Overlay.ShowBinds();
		//}

		Transform.Rotation = Rotation.FromYaw( EyeAngles.ToRotation().Yaw() );

	}

	protected override void OnPreRender()
	{
		PlayerInput?.OnPreRender();
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
