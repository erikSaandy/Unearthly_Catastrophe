using Dungeon;
using Saandy;
using Sandbox;
using Sandbox.Citizen;
using System;
using System.Security.Cryptography;

public class Zombie : Component, IKillable
{
	public enum State
	{
		Patrol,
		Aggro,
		Idle
	}

	public State MoveState { get; set; } = State.Patrol;

	[Sync] public LifeState LifeState { get; private set; } = LifeState.Alive;

	[Sync] public RealTimeSince TimeSinceDeath { get; private set; }

	private float LineOfSightSquared => (MoveState == State.Aggro) ? 120000 : 110000;
	private float AggroDistance { get; set; } = 45000;

	[Category("Components")][Property] public NavMeshAgent Agent { get; private set; }
	[Category( "Components" )][Property] public CitizenAnimationHelper Animator { get; private set; }
	[Category( "Components" )][Property] public CharacterController Controller { get; private set; }

	public float MaxHealth => 100;
	public float Health { get; private set; }

	[Property] public float WalkSpeed { get; set; } = 70;
	[Property] public float RunSpeed { get; set; } = 120;
	[Sync] public Vector3 NestPosition { get; set; }
	[Sync] public Vector3 WantedMoveDirection { get; set; }
	[Sync] public Rotation WantedRotation { get; set; }
	[Sync] public Angles EyeAngles { get; set; }

	public TimeSince TimeSinceUpdateTarget { get; private set; } = 30;
	public TimeSince TimeSinceTrace { get; private set; } = 30;
	public TimeSince TimeSinceAggro { get; private set; } = 30;

	private GameObject LastAggroedPlayer { get; set; } = default;

	protected override void OnAwake()
	{
		base.OnAwake();

		Health = MaxHealth;
		LifeState = LifeState.Alive;

		Agent = Components.Get<NavMeshAgent>();
		Animator = Components.Get<CitizenAnimationHelper>();
		Controller = Components.Get<CharacterController>();

		// On spawn
		//StartPatroling( Transform.Position );

	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		Gizmo.Draw.Color = Color.Red;
		if(MoveState == State.Aggro)
		{
			Gizmo.Draw.SolidSphere( 0, 32 );
		}

	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		Animator.IsGrounded = Controller.IsOnGround;
		Animator.WithVelocity( Controller.Velocity );
		Animator.WithLook( EyeAngles.Forward, 1, .8f, .5f );

		if ( IsProxy ) { return; }

		if ( Input.Down("jump"))
		{
			Transform.Position = DungeonGenerator.SpawnedRooms[0].GameObject.Transform.Position;
			ChangeTargetPosition();
			StartPatroling( Transform.Position );
		}

		// Patrol nest sees a player, continue chasing player until line of sight is broken.
		// Patrol last seen area 

		float speed = WalkSpeed;
		UpdateMovement( ref speed );

		// Do raycasts
		if ( WantedMoveDirection != 0 && TimeSinceTrace > 3 )
		{
			TimeSinceTrace = 0;
			IEnumerable<SceneTraceResult> trace = Scene.Trace.Ray( Transform.Position, Transform.Position + (WantedMoveDirection * 64) ).IgnoreGameObjectHierarchy( GameObject ).UsePhysicsWorld().RunAll();

			foreach ( SceneTraceResult result in trace )
			{
				if ( result.GameObject != null )
				{
					if ( result.GameObject.Components.TryGet<DoorComponent>( out DoorComponent door ) )
					{
						door.Open();
					}
				}
			}
		}

	}

	private void UpdateMovement(ref float speed)
	{
		float turnSpeed = 3.5f;
		float friction = 10f;

		if ( MoveState == State.Patrol )
		{
			GameObject playerToAggro = FindPlayerToAggro();
			if ( playerToAggro != null )
			{
				AggroPlayer( playerToAggro );
				return;
			}

			// Patrol to new locations
			if ( TimeSinceUpdateTarget > 10 )
			{
				ChangeTargetPosition();

			}

			WantedMoveDirection = (Agent.GetLookAhead( 1 ) - Transform.Position).Normal;

			Vector3 targetPos = Agent.TargetPosition ?? default;

			if ( targetPos != default )
			{
				if ( Vector3.DistanceBetween( Transform.Position, targetPos ) < 25 )
				{
					MoveState = State.Idle;
				}
			}

		}

		else if ( MoveState == State.Idle )
		{
			speed = 0;
			Controller.Velocity.Set( 0, 0, Controller.Velocity.z );

			GameObject playerToAggro = FindPlayerToAggro();
			if ( playerToAggro != null )
			{
				AggroPlayer( playerToAggro );
				return;
			}

			Log.Info( "idle" );
			if ( TimeSinceUpdateTarget > 5 )
			{

				StartPatroling( NestPosition );
				TimeSinceUpdateTarget = 0;
			}

		}

		else if ( MoveState == State.Aggro )
		{
			speed = RunSpeed;
			friction = 6;
			turnSpeed = 15;

			if ( TimeSinceUpdateTarget > 1 )
			{
				GameObject playerToAggro = FindPlayerToAggro();
				if ( playerToAggro != null )
				{
					AggroPlayer( playerToAggro );
					return;
				}

				// Stop aggroing.
				StartPatroling( Transform.Position );
			}

			WantedMoveDirection = (Agent.GetLookAhead( 1 ) - Transform.Position).Normal;

		}

		if ( Controller.IsOnGround )
		{
			Controller.ApplyFriction( friction );
		}

		float angle = Vector3.VectorAngle( WantedMoveDirection ).yaw;
		EyeAngles = Rotation.Lerp( EyeAngles, new Angles( 0, angle, 0 ), Time.Delta * turnSpeed );
		Transform.Rotation = EyeAngles;
		Controller.Accelerate( EyeAngles.Forward * speed );
		Controller.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;
		Controller.Move();

	}

	private Vector3 ChangeTargetPosition()
	{
		TimeSinceUpdateTarget = 0;
		return Scene.NavMesh.GetRandomPoint( NestPosition, 700 ) ?? Transform.Position;
	}

	private void StartPatroling ( Vector3 newNest )
	{
		NestPosition = newNest;
		
		Agent.MoveTo( ChangeTargetPosition() );
		MoveState = State.Patrol;
	}

	private void AggroPlayer(GameObject playerObject)
	{
		Log.Info( "aggro" );
		Agent.MoveTo( playerObject.Transform.Position );
		MoveState = State.Aggro;
		LastAggroedPlayer = playerObject;
		TimeSinceAggro = 0;
		TimeSinceUpdateTarget = 0;
	}

	private GameObject FindPlayerToAggro()
	{
		foreach ( Guid guid in LethalGameManager.Instance.ConnectedPlayers )
		{
			GameObject playerObject = Scene.Directory.FindByGuid( guid );
			float dstToPlayerSqr = Vector3.DistanceBetweenSquared( playerObject.Transform.Position, Transform.Position );

			// Player is within line of sight
			if ( dstToPlayerSqr < LineOfSightSquared )
			{
				// still within line of sight...
				if(TimeSinceAggro < 8 && playerObject == LastAggroedPlayer)
				{
					return LastAggroedPlayer;
				}

				SceneTraceResult trace = Scene.Trace.Ray( Transform.Position, playerObject.Transform.Position ).WithoutTags( "item" ).IgnoreGameObjectHierarchy( playerObject ).UseHitboxes().Run();

				// LOS to player
				if ( trace.GameObject == playerObject )
				{
					return playerObject;
				}

				// Player is withing aggro range
				if ( dstToPlayerSqr < AggroDistance )
				{

					return playerObject;

				}
			}

		}

		return null;
	}

	public virtual void Kill()
	{
		if ( LifeState == LifeState.Dead ) { return; }
		TakeDamage( Health + 100, GameObject.Id );
	}

	public virtual void TakeDamage( float damage, Guid attackerId )
	{
		if ( LifeState == LifeState.Dead )
			return;

		if ( IsProxy )
			return;

		Health = MathF.Max( Health - damage, 0f );

		if ( Health <= 0f )
		{
			LifeState = LifeState.Dead;
			//Ragdoll.Ragdoll();
			//OnKilled( Scene.Directory.FindByGuid( attackerId ) );
			TimeSinceDeath = 0;
		}
	}
}
