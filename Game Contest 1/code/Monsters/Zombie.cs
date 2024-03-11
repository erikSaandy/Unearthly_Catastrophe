using Dungeon;
using Saandy;
using Sandbox;
using Sandbox.Citizen;
using System;
using System.Security.Cryptography;

public class Zombie : Monster
{
	public enum State
	{
		Patrol,
		Aggro,
		Idle
	}

	public State MoveState { get; set; } = State.Patrol;

	private float LineOfSightSquared => MoveState == State.Aggro ? 130000 : 110000;
	private float AggroDistance { get; set; } = 37000;
	private float AttackDistance { get; set; } = 4000;

	[Category("Components")][Property] public NavMeshAgent Agent { get; private set; }
	[Category( "Components" )][Property] public CitizenAnimationHelper Animator { get; private set; }
	[Category( "Components" )][Property] public CharacterController Controller { get; private set; }

	[Property] public float WalkSpeed { get; set; } = 70;
	[Property] public float RunSpeed { get; set; } = 120;
	[Sync] public Vector3 NestPosition { get; set; }
	[Sync] public Vector3 WantedMoveDirection { get; set; }
	[Sync] public Rotation WantedRotation { get; set; }
	[Sync] public Angles EyeAngles { get; set; }

	public TimeSince TimeSinceUpdateTarget { get; private set; } = 30;
	public TimeSince TimeSinceTrace { get; private set; } = 30;
	public TimeSince TimeSinceAggro { get; private set; } = 30;
	public RealTimeSince TimeSinceAttack { get; set; } = 0;

	private GameObject LastAggroedPlayer { get; set; } = null;

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

		Gizmo.Draw.Color = Color.Yellow;
		Gizmo.Draw.SolidCylinder( 0, Vector3.Up * 4, MathF.Sqrt( LineOfSightSquared ) );

		Gizmo.Draw.Color = Color.Red;
		Gizmo.Draw.SolidCylinder( 0, Vector3.Up * 4, MathF.Sqrt( AggroDistance ) );

		if (MoveState == State.Aggro)
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

		//if ( Input.Down("jump"))
		//{
		//	Transform.Position = DungeonGenerator.SpawnedRooms[0].GameObject.Transform.Position;
		//	ChangeTargetPosition();
		//	StartPatroling( Transform.Position );
		//}

		// Patrol nest sees a player, continue chasing player until line of sight is broken.
		// Patrol last seen area 

		UpdateMovement();

		// Do door raycasts
		if ( WantedMoveDirection != 0 && TimeSinceTrace > 3 )
		{
			TimeSinceTrace = 0;
			IEnumerable<SceneTraceResult> trace = Scene.Trace.Ray( Animator.EyeSource.Transform.Position, Animator.EyeSource.Transform.Position + (WantedMoveDirection * 32) ).IgnoreGameObjectHierarchy( GameObject ).UsePhysicsWorld().RunAll();	

			foreach ( SceneTraceResult result in trace )
			{
				if ( result.GameObject != null )
				{
					if ( result.GameObject.Components.TryGet<DoorComponent>( out DoorComponent door ) )
					{
						if( !door.IsLocked )
						{
							door.Open();
						}
					}
				}
			}
		}

	}

	private void UpdateMovement()
	{
		float speed = WalkSpeed;
		float turnSpeed = 3.5f;
		float friction = 10f;

		if ( MoveState == State.Patrol )
		{
			DoPatrol(ref speed, ref turnSpeed, ref friction );
		}
		else if ( MoveState == State.Idle )
		{
			DoIdle( ref speed, ref turnSpeed, ref friction );
		}
		else if ( MoveState == State.Aggro )
		{
			DoAggro( ref speed, ref turnSpeed, ref friction );
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
		LastAggroedPlayer = null;

		Agent.MoveTo( ChangeTargetPosition() );
		MoveState = State.Patrol;
	}

	private void AggroPlayer(GameObject playerObject)
	{
		Log.Info( $"{GameObject.Name} aggro" );
		Agent.MoveTo( playerObject.Transform.Position );
		MoveState = State.Aggro;
		LastAggroedPlayer = playerObject;
		TimeSinceAggro = 0;
		TimeSinceUpdateTarget = 0;
		NestPosition = playerObject.Transform.Position;
	}

	private GameObject FindPlayerToAggro(out float dstToPlayerSqr)
	{
		dstToPlayerSqr = 0;

		foreach ( Guid guid in LethalGameManager.Instance.ConnectedPlayers )
		{
			GameObject playerObject = Scene.Directory.FindByGuid( guid );

			if(playerObject == null) { return null; }

			// Don't aggro dead players.
			if ( playerObject.Tags.Has("dead") ) { return null; }

			dstToPlayerSqr = Vector3.DistanceBetweenSquared( playerObject.Transform.Position, Transform.Position );
			//Log.Info( dstToPlayerSqr );

			// Allways aggro when player is within attack range.
			if ( dstToPlayerSqr < AttackDistance )
			{
				return playerObject;
			}

			// Player is within line of sight
			if ( dstToPlayerSqr < LineOfSightSquared )
			{
				Vector3 dir = (playerObject.Transform.Position - Transform.Position).Normal;
				float dot = Vector3.Dot( Transform.Rotation.Forward, dir );

				// If player is withing aggro range, continue chase.
				if ( LastAggroedPlayer == playerObject && dstToPlayerSqr < AggroDistance )
				{
					Log.Info( "chase" );
					return playerObject;
				}

				// Can't aggro if looking the other way.
				if ( dot < 0.5f )
				{
					return null;
				}

				// Player is withing aggro range
				if ( dstToPlayerSqr < AggroDistance )
				{
					return playerObject;
				}

				// still aggro, and player is not far away enough.
				if (TimeSinceAggro < 8 && playerObject == LastAggroedPlayer)
				{
					return LastAggroedPlayer;
				}

				// Scene.Trace.Ray( Transform.Position, Transform.Position + (WantedMoveDirection * 64) ).IgnoreGameObjectHierarchy( GameObject ).UsePhysicsWorld().RunAll();
				SceneTraceResult trace = Scene.Trace.Ray( Animator.EyeSource.Transform.Position, Animator.EyeSource.Transform.Position + (dir * LineOfSightSquared) )
					.IgnoreGameObjectHierarchy( GameObject.Root )
					//.UseHitboxes()
					.UseRenderMeshes()
					.WithoutTags( "item", "door" )
					.Run();

				// LOS to player
				if ( trace.GameObject == playerObject )
				{
					return playerObject;
				}
			}

		}

		return null;
	}

	private void DoPatrol( ref float speed, ref float turnSpeed, ref float friction)
	{
		GameObject playerToAggro = FindPlayerToAggro( out float dstToPlayerSqr );
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

	private void DoIdle(ref float speed, ref float turnSpeed, ref float friction )
	{
		speed = 0;
		Controller.Velocity.Set( 0, 0, Controller.Velocity.z );

		GameObject playerToAggro = FindPlayerToAggro( out float dstToPlayerSqr );
		if ( playerToAggro != null )
		{
			AggroPlayer( playerToAggro );

			return;
		}

		Log.Info( $"{GameObject.Name} idle" );
		if ( TimeSinceUpdateTarget > 5 )
		{

			StartPatroling( NestPosition );
			TimeSinceUpdateTarget = 0;
		}
	}

	private void DoAggro( ref float speed, ref float turnSpeed, ref float friction )
	{
		speed = RunSpeed;
		friction = 6;
		turnSpeed = 15;

		GameObject playerToAggro = FindPlayerToAggro( out float dstToPlayerSqr );

		if ( TimeSinceUpdateTarget > 1 )
		{
			if ( playerToAggro != null )
			{
				AggroPlayer( playerToAggro );
			}
			else
			{
				// Stop aggroing.
				Log.Info( $"{GameObject.Name} stop aggroing" );
				StartPatroling( NestPosition );
			}
		}

		//Log.Info( dstToPlayerSqr );
		// Attack player
		if ( playerToAggro != null && TimeSinceAttack > 2 && dstToPlayerSqr < AttackDistance )
		{
			TimeSinceAttack = 0;
			Log.Info( $"{GameObject.Name} ATTACK" );
			playerToAggro.Components.Get<IKillable>().TakeDamage( 40, GameObject.Id, Transform.Rotation.Up * 300 );
		}

		WantedMoveDirection = (Agent.GetLookAhead( 1 ) - Transform.Position).Normal;

	}

	public override void Kill()
	{
		if ( LifeState == LifeState.Dead ) { return; }
		TakeDamage( Health + 100, GameObject.Id );
	}

	public override void TakeDamage( float damage, Guid attackerId, Vector3 impulseForce = default )
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
