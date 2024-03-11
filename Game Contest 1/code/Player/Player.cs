﻿
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
	public CitizenAnimationHelper Animator { get; set; }

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

		PlayerInput = new PlayerInput( this );

		Animator = Components.Get<CitizenAnimationHelper>();
		Controller = Components.Get<CharacterController>();
		Inventory = Components.Get<InventoryComponent>();
		EnergyBar = Components.Get<EnergyBarComponent>();
		Ragdoll = Components.Get<RagdollController>( true );

		if ( GameObject.IsProxy ) {

			HudObject.Destroy();
			CameraController.Camera.Destroy();
			return; 
		}

		LethalGameManager.OnStartLoadMoon += OnStartLoadMoon;
		LethalGameManager.OnLoadedMoon += OnLoadedMoon;

		CurrentHud = HudObject.Components.Get<AliveHud>(true);
		CurrentHud.Enabled = true;

		LethalGameManager.OnPlayerConnected( GameObject.Id );

		Animator.Target.SetBodyGroup( "head", 1 );

	}

	protected override void OnEnabled()
	{
		base.OnEnabled();
	}

	protected override void OnUpdate()
	{

		Animator.HoldType = CurrentHoldType;

		if ( GameObject.IsProxy ) { return; }

		//

		//if(Input.Pressed("flashlight"))
		//{
		//	Kill();
		//}

		PlayerInput?.UpdateInput();

	}


	[Broadcast]
	public void TakeDamage( float damage, Guid attackerId )
	{
		if ( LifeState == LifeState.Dead )
			return;

		if ( IsProxy )
			return;

		TimeSinceDamaged = 0f;
		Health = MathF.Max( Health - damage, 0f );

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
		if ( attacker.IsValid() )
		{
			/*
			var player = attacker.Components.GetInAncestorsOrSelf<Player>();
			if ( player.IsValid() )
			{
				var chat = Scene.GetAllComponents<Chat>().FirstOrDefault();

				if ( chat.IsValid() )
					chat.AddTextLocal( "💀️", $"{player.Network.OwnerConnection.DisplayName} has killed {Network.OwnerConnection.DisplayName}" );
			}
			*/
		}

		if ( IsProxy )
			return;

		Animator.Target.SetBodyGroup( "head", 0 );
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
		if ( IsProxy ) { return; }

		Controller.Velocity = 0;
		Controller.Acceleration = 0;
		Ragdoll.Unragdoll(); 
		MoveToSpawnPoint();
		LifeState = LifeState.Alive;
		Health = MaxHealth;
		PlayerInput = new PlayerInput( this );
		Animator.Target.SetBodyGroup( "head", 1 );

	}

	private void MoveToSpawnPoint()
	{
		var spawnpoints = Scene.GetAllComponents<SpawnPoint>();
		var randomSpawnpoint = LethalGameManager.Random.FromList( spawnpoints.ToList() );

		//Transform.Rotation = randomSpawnpoint.Transform.Rotation;
		Transform.Position = LethalGameManager.Instance.Ship.Transform.Position;
		//Transform.Position = randomSpawnpoint.Transform.Position;
		//EyeAngles = Transform.Rotation;
	}

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
		if ( Controller == null ) { return; }
		if ( Animator == null ) { return; }

		Animator.IsGrounded = Controller.IsOnGround;
		Animator.WithVelocity( Controller.Velocity );
		Animator.WithLook( EyeAngles.Forward, 1, .8f, .5f );
		//Animator.DuckLevel = 1f;

		base.OnFixedUpdate();

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

		Controller.Move();

		if ( LastGroundObject == LethalGameManager.Instance.Ship.GameObject && LethalGameManager.Instance.Ship.IsMoving )
		{
			Controller.Velocity = LethalGameManager.Instance.Ship.Controller.Velocity;
			Controller.Move();
			//Controller.Accelerate( LethalGameManager.Instance.Ship.Velocity );
			//Controller.MoveTo( Transform.Position + (LethalGameManager.Instance.Ship.Velocity), true );
			//Controller.Velocity += LethalGameManager.Instance.Ship.Controller.Velocity;
			//Controller.Velocity = (LethalGameManager.Instance.Ship.Controller.Velocity);
		}

		if ( LastGroundObject == LethalGameManager.Instance.Ship.GameObject )
		{
			//Controller.Accelerate( LethalGameManager.Instance.Ship.Velocity );
			//Controller.MoveTo( Transform.Position + (LethalGameManager.Instance.Ship.Velocity), true );
			//Controller.Velocity -= LethalGameManager.Instance.Ship.Controller.Velocity;

		}
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
