using Dungeon;
using Saandy;
using Sandbox;
using Sandbox.Citizen;
using System;
using System.Numerics;
using System.Security.Cryptography;

public class Zombie : Monster
{
	public enum State
	{
		Patrol,
		Aggro,
		Idle
	}

	[Property] public State MoveState { get; set; } = State.Patrol;

	private float LineOfSightSquared => MoveState == State.Aggro ? 130000 : 110000;
	private float AggroDistance { get; set; } = 37000;
	private float AttackDistance { get; set; } = 4000;

	[Category("Components")][Property] public NavMeshAgent Agent { get; private set; }
	[Category( "Components" )][Property] public CitizenAnimationHelper Animator { get; private set; }
	[Category( "Components" )][Property] public CharacterController Controller { get; private set; }
	[Category( "Components" )][Property] public RagdollController Ragdoll { get; private set; }
	[Category( "Components" )][Property] public CapsuleCollider PhysicsCollider { get; private set; }

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

	public TimeSince TimeSinceGrowl { get; private set; } = 30;
	[Category( "Sounds" )][Property] public SoundEvent IdleGrowlSound { get; private set; }
	[Category( "Sounds" )][Property] public SoundEvent AggroSound { get; private set; }

	private Player LastAggroedPlayer { get; set; } = null;

	protected override void OnAwake()
	{
		base.OnAwake();

		Health = MaxHealth;
		LifeState = LifeState.Alive;

		Agent = Components.Get<NavMeshAgent>();
		Animator = Components.Get<CitizenAnimationHelper>();
		Controller = Components.Get<CharacterController>();

	}

	protected override void OnStart()
	{
		base.OnStart();

		if(IsProxy) { return; }

		Animator.Height = Math2d.Map( (float)Random.Shared.NextDouble(), 0, 1, 0.7f, 1.2f );

		StartPatroling( Transform.Position );

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

	protected override void OnUpdate()
	{
		base.OnUpdate();

		Animator.IsGrounded = Controller.IsOnGround;
		Animator.WithVelocity( Controller.Velocity * (MoveState == State.Aggro ? 2 : 1) );
		Animator.WithLook( EyeAngles.Forward, 1, .8f, .5f );
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( IsProxy ) { return; }

		if(LifeState == LifeState.Dead) { return; }

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
			IEnumerable<SceneTraceResult> trace = Scene.Trace.Ray( Animator.EyeSource.Transform.Position, Animator.EyeSource.Transform.Position + (WantedMoveDirection * 32) )
				.IgnoreGameObjectHierarchy( GameObject )
				.WithoutTags("monster")
				.UsePhysicsWorld()
				.RunAll();	

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

	private void ChangeTargetPosition()
	{
		TimeSinceUpdateTarget = 0;
		Agent.MoveTo( Scene.NavMesh.GetRandomPoint( NestPosition, 750 ) ?? Transform.Position );
	}

	private void StartPatroling ( Vector3 newNest )
	{
		NestPosition = newNest;
		LastAggroedPlayer = null;

		ChangeTargetPosition();
		MoveState = State.Patrol;
	}

	private void AggroPlayer(Player player)
	{
		if(LastAggroedPlayer != player)
		{
			OnStartAggro();
		}

		Agent.MoveTo( player.Transform.Position );
		MoveState = State.Aggro;
		LastAggroedPlayer = player;
		TimeSinceAggro = 0;
		TimeSinceUpdateTarget = 0;
		NestPosition = player.Transform.Position;
	}

	[Broadcast]
	private void OnStartAggro()
	{
		Sound.Play( AggroSound, Transform.Position );
	}

	[Broadcast]
	private void OnGrowl()
	{
		Sound.Play( IdleGrowlSound, Transform.Position );
	}


	private Player FindPlayerToAggro(out float dstToPlayerSqr)
	{
		dstToPlayerSqr = 0;

		if ( TimeSinceAggro < 6 && LastAggroedPlayer?.LifeState == LifeState.Alive )
		{
			dstToPlayerSqr = Vector3.DistanceBetweenSquared( LastAggroedPlayer.Transform.Position, Transform.Position );
			return LastAggroedPlayer;
		}

		List<Player> players = LethalGameManager.Instance.AlivePlayers.ToList();

		foreach ( Player player in players )
		{

			//if( player == null) { return null; }

			dstToPlayerSqr = Vector3.DistanceBetweenSquared( player.Transform.Position, Transform.Position );
			//Log.Info( dstToPlayerSqr );

			// Allways aggro when player is within attack range.
			if ( dstToPlayerSqr < AttackDistance )
			{
				return player;
			}

			// Player is within line of sight
			if ( dstToPlayerSqr < LineOfSightSquared )
			{
				Vector3 dir = (player.Transform.Position - Transform.Position).Normal;
				float dot = Vector3.Dot( Transform.Rotation.Forward, dir );

				// If player is withing aggro range, continue chase.
				if ( LastAggroedPlayer == player && dstToPlayerSqr < AggroDistance )
				{
					return player;
				}

				// Can't aggro if looking the other way.
				if ( dot < 0.6f )
				{
					return null;
				}

				// Player is withing aggro range
				if ( dstToPlayerSqr < AggroDistance )
				{
					return player;
				}

				// Scene.Trace.Ray( Transform.Position, Transform.Position + (WantedMoveDirection * 64) ).IgnoreGameObjectHierarchy( GameObject ).UsePhysicsWorld().RunAll();
				SceneTraceResult trace = Scene.Trace.Ray( Animator.EyeSource.Transform.Position, Animator.EyeSource.Transform.Position + (dir * LineOfSightSquared) )
					.IgnoreGameObjectHierarchy( GameObject.Root )
					//.UseHitboxes()
					.UseRenderMeshes()
					.WithoutTags( "item", "door", "monster" )
					.Run();		

				// LOS to player
				if ( trace.GameObject == player.GameObject )
				{
					return player;
				}
			}

		}

		return null;
	}

	private void DoPatrol( ref float speed, ref float turnSpeed, ref float friction)
	{
		if( TimeSinceGrowl > 6)
		{
			OnGrowl();
			TimeSinceGrowl = LethalGameManager.Random.Float( -1f, 1.5f );
		}

		Player playerToAggro = FindPlayerToAggro( out float dstToPlayerSqr );
		if ( playerToAggro != null )
		{
			AggroPlayer( playerToAggro );
			return;
		}

		// Patrol to new locations
		if ( TimeSinceUpdateTarget > 10 )
		{
			//Log.Info( "new target" );
			ChangeTargetPosition();

		}

		WantedMoveDirection = (Agent.GetLookAhead( 1 ) - Transform.Position).Normal;
	}

	private void DoIdle(ref float speed, ref float turnSpeed, ref float friction )
	{
		speed = 0;
		Controller.Velocity.Set( 0, 0, Controller.Velocity.z );

		Player playerToAggro = FindPlayerToAggro( out float dstToPlayerSqr );
		if ( playerToAggro != null )
		{
			AggroPlayer( playerToAggro );

			return;
		}

		//Log.Info( $"{GameObject.Name} idle" );
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

		Player playerToAggro = FindPlayerToAggro( out float dstToPlayerSqr );

		if ( TimeSinceUpdateTarget > 0.1f )
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

		// Attack player
		if ( playerToAggro != null && TimeSinceAttack > 2 && dstToPlayerSqr < AttackDistance )
		{
			TimeSinceAttack = 0;
			Log.Info( $"Zombie attacked {playerToAggro.GameObject.Name}" );
			Animator?.TriggerDeploy();
			Controller.Punch( Vector3.Up * 150 );
			playerToAggro.Components.Get<IKillable>().TakeDamage( 40, GameObject.Id, Transform.Rotation.Up * 300 );
		}

		WantedMoveDirection = (Agent.GetLookAhead( 1 ) - Transform.Position).Normal;

		if(dstToPlayerSqr < AttackDistance)
		{
			speed = 0.1f;
		}

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
			Tags.Add( "dead" );
			LifeState = LifeState.Dead;
			Ragdoll.Ragdoll();
			Agent.Enabled = false;
			PhysicsCollider.Enabled = false;
			//OnKilled( Scene.Directory.FindByGuid( attackerId ) );
			TimeSinceDeath = 0;
		}
	}
}
