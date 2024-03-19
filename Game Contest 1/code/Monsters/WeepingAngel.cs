using Dungeon;
using Saandy;
using Sandbox;
using Sandbox.Citizen;
using System;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Security.Cryptography;
using static Zombie;

public class WeepingAngel : Monster
{
	public enum BehaviourState
	{
		Spawning,
		Idle,
		Waking,
		Hunting,
		Aggro
	}

	[Property] public BehaviourState State { get; set; } = BehaviourState.Spawning;

	private float LineOfSight { get; set; } = 500000;
	private float AggroDistance { get; set; } = 115000;
	private float AttackDistance { get; set; } = 4000;


	[Category( "Components" )][Property] public ModelRenderer Renderer { get; private set; }
	[Category( "Components" )][Property] public NavMeshAgent Agent { get; private set; }
	[Category( "Components" )][Property] public CharacterController Controller { get; private set; }
	[Category( "Components" )][Property] public ModelCollider Collider { get; private set; }

	[Property] public List<Model> ModelStates { get; set; }

	[Property] public float RunSpeed { get; set; } = 240;

	private TimeSince TimeSinceStateChange { get; set; } = 0;

	private TimeSince TimeSinceUpdate { get; set; } = 0;
	private TimeSince TimeSinceDoorOpenUpdate { get; set; } = 0;
	private TimeSince TimeSinceAttack { get; set; } = 0;
	private TimeSince TimeSinceMove { get; set; } = 0;

	const float IDLE_TIME = 5;
	const float WAKEUP_TIME = 5;

	[Sync] public Vector3 WantedMoveDirection { get; set; }
	[Sync] public Angles EyeAngles { get; set; }
	[Property] public GameObject EyeSource { get; set; }
	private Player FocusedPlayer { get; set; } = null;

	[Sync] public Vector3 TargetPosition { get; set; }


	private TimeSince TimeSinceSeenUpdate { get; set; } = 0;
	[Sync] private bool HasBeenSeen { get; set; } = false;
	[Sync] public bool IsSeen { get; private set; } = false;

	private bool WasShown { get; set; } = false;
	private bool WasHidden { get; set; } = false;

	protected override void OnAwake()
	{
		base.OnAwake();

		if ( IsProxy ) { return; }

		Health = MaxHealth;
		LifeState = LifeState.Alive;

		Agent = Components.Get<NavMeshAgent>();
		Controller = Components.Get<CharacterController>();

	}

	protected override void OnStart()
	{
		base.OnStart();

		GameObject.BreakFromPrefab();

		TimeSinceStateChange = 0;

		if ( IsProxy ) { return; }

	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		Gizmo.Draw.Color = Color.Yellow;
		Gizmo.Draw.SolidCylinder( 0, Vector3.Up * 4, MathF.Sqrt( LineOfSight ) );

		Gizmo.Draw.Color = Color.Red;
		Gizmo.Draw.SolidCylinder( 0, Vector3.Up * 4, MathF.Sqrt( AggroDistance ) );

		if ( State == BehaviourState.Aggro )
		{
			Gizmo.Draw.SolidSphere( 0, 32 );
		}

	}

	protected override void OnUpdate()
	{
		base.OnUpdate();



	}

	[Broadcast]
	private void MarkAsSeen()
	{
		HasBeenSeen = true;
	}

	private void UpdateIsSeen()
	{

		if ( TimeSinceSeenUpdate > 0.2f )
		{
			TimeSinceSeenUpdate = 0;

			if ( Scene.Camera.CanSeeBounds( Renderer.Bounds ) )
			{
				MarkAsSeen();
			}

			if ( IsProxy ) { return; }

			WasShown = false;
			WasHidden = false;

			if ( HasBeenSeen ) {
				if ( !IsSeen ) {
					WasShown = true;
					OnShown();
				}

				IsSeen = true;
				HasBeenSeen = false;
			}
			else
			{
				if ( IsSeen ) {
					WasHidden = true;
					OnHidden();
				}

				IsSeen = false;
			}

		}
	}

	[Broadcast]
	private void OnShown()
	{
	}


	[Broadcast]
	private void OnHidden()
	{
		//modelState = (++modelState % ModelStates.Count);
		//Renderer.Model = ModelStates[modelState];

	}

	private void GleamAtFocusedPlayer()
	{
		if ( FocusedPlayer == null ) { return; }

		// -1 is left, 1 is right
		float dot = Vector3.Dot( Transform.Rotation.Left, (Transform.Position - FocusedPlayer.Transform.Position).Normal );
		float sign = MathF.Sign( dot );

		int state = sign == -1 ? 1 : 2;

		Renderer.Model = ModelStates[state];

		EyeAngles = Transform.Rotation.RotateAroundAxis( Vector3.Down, 20 * sign );


	}


	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		//Log.Info( IsSeen );

		if ( IsProxy ) { return; }

		if ( LifeState == LifeState.Dead ) { return; }
		UpdateMovement();

		// Do door raycasts
		if ( TimeSinceDoorOpenUpdate > 25 )
		{
			TimeSinceDoorOpenUpdate = 0;
			IEnumerable<SceneTraceResult> trace = Scene.Trace.Ray( EyeSource.Transform.Position, EyeSource.Transform.Position + (WantedMoveDirection * 32) )
				.IgnoreGameObjectHierarchy( GameObject )
				.WithoutTags( "monster" )
				.UsePhysicsWorld()
				.RunAll();

			foreach ( SceneTraceResult result in trace )
			{
				if ( result.GameObject != null )
				{
					if ( result.GameObject.Components.TryGet<DoorComponent>( out DoorComponent door ) )
					{
						if ( !door.IsLocked )
						{
							door.Open();
						}
					}
				}
			}
		}

	}

	private void TargetPlayer( Player player )
	{
		TimeSinceUpdate = 0;
		Agent.MoveTo( player.Transform.Position );
	}

	private void AggroPlayer( Player player )
	{
		TargetPlayer( player );
		State = BehaviourState.Aggro;
		TimeSinceUpdate = 0;
	}

	private Player FindPlayerToAggro( out float dstToPlayerSqr )
	{
		dstToPlayerSqr = 0;

		if ( FocusedPlayer?.LifeState == LifeState.Alive )
		{
			dstToPlayerSqr = Vector3.DistanceBetweenSquared( FocusedPlayer.Transform.Position, Transform.Position );
			if ( dstToPlayerSqr < LineOfSight )
			{
				// Stick to aggroed player until dead or not within los.
				return FocusedPlayer;
			}
		}

		List<Player> players = LethalGameManager.Instance.AlivePlayers.ToList();

		foreach ( Player player in players )
		{

			//if( player == null) { return null; }

			dstToPlayerSqr = Vector3.DistanceBetweenSquared( player.Transform.Position, Transform.Position );
			//Log.Info( dstToPlayerSqr );

			// Player is within line of sight
			if ( dstToPlayerSqr < LineOfSight )
			{
				Vector3 dir = (player.Transform.Position - Transform.Position).Normal;

				// Player is withing aggro range
				if ( dstToPlayerSqr < AggroDistance )
				{
					return player;
				}

				// Scene.Trace.Ray( Transform.Position, Transform.Position + (WantedMoveDirection * 64) ).IgnoreGameObjectHierarchy( GameObject ).UsePhysicsWorld().RunAll();
				SceneTraceResult trace = Scene.Trace.Ray( EyeSource.Transform.Position, EyeSource.Transform.Position + (dir * LineOfSight) )
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

	private void DoSettle()
	{
		Vector3 settlePos = Transform.Position;
		Log.Info( GameObject.Name + " settling.." );

		int i = 0;
		while ( i < 10 )
		{

			float angle = (LethalGameManager.Random.Next( 0, 100 ) * 0.01f) * MathF.Tau;
			Vector3 dir = new Vector3( (float)Math.Sin( angle ), (float)Math.Cos( angle ), 0 );
			float dst = LineOfSight;

			IEnumerable<SceneTraceResult> backTrace = Scene.Trace.Sphere( 64, Transform.Position, EyeSource.Transform.Position + (dir * dst) )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "monster" )
			.UseRenderMeshes()
			.RunAll();

			foreach ( SceneTraceResult result in backTrace )
			{
				if ( result.Hit && !result.Tags.Contains( "door" ) && !result.Tags.Contains( "entrance_door" ) )
				{
					float dot = Vector3.Dot( Vector3.Up, result.Normal );

					//Log.Info( result.GameObject?.Name );
					settlePos = result.HitPosition;
					EyeAngles = new Angles( 0, Math2d.Angle3D( Vector3.Forward, result.Normal, Vector3.Up ), 0 );
					State = BehaviourState.Idle;
					break;
				}
			}

			if ( State == BehaviourState.Idle )
			{
				break;
			}

			i++;
		}

		Transform.Rotation = EyeAngles;// + EyeAngles.Forward * 32;
		Controller.Radius = 20;
		Controller.MoveTo( settlePos, true );
		Controller.Radius = 16;
		Transform.Position += EyeAngles.Forward * 12;

		SceneTraceResult feetTrace = Scene.Trace.Ray( Transform.Position, Transform.Position + Vector3.Down * 64 )
		.IgnoreGameObjectHierarchy( GameObject )
		.WithoutTags( "monster" )
		.Run();

		if ( feetTrace.Hit )
		{
			Transform.Position = Transform.Position.WithZ( feetTrace.HitPosition.z );
		}

		TimeSinceStateChange = 0;

	}

	private void UpdateMovement()
	{
		float speed = RunSpeed;
		float turnSpeed = 3.5f;
		float friction = 10f;

		switch ( State )
		{
			case BehaviourState.Spawning: DoSpawning(); break;
			case BehaviourState.Idle: DoIdle( ref speed, ref turnSpeed, ref friction ); break;
			case BehaviourState.Waking: DoWaking( ref speed, ref turnSpeed, ref friction ); break;
			case BehaviourState.Hunting: DoHunting( ref speed, ref turnSpeed, ref friction ); break;
			case BehaviourState.Aggro: DoAggro( ref speed, ref turnSpeed, ref friction ); break;
		}

	}

	private void DoSpawning()
	{
		if(TimeSinceStateChange > 3)
		{
			DoSettle();
			TimeSinceStateChange = 0;
			State = BehaviourState.Idle;
		}
	}

	private void DoIdle( ref float speed, ref float turnSpeed, ref float friction )
	{

		if ( TimeSinceStateChange > IDLE_TIME )
		{
			State = BehaviourState.Waking;
			TimeSinceStateChange = 0;
		}
	}

	private void DoWaking( ref float speed, ref float turnSpeed, ref float friction )
	{
		if ( TimeSinceStateChange > WAKEUP_TIME )
		{
			TimeSinceStateChange = 0;
			State = BehaviourState.Hunting;
			return;
		}

		UpdateIsSeen();

		// Get focused player
		if ( FocusedPlayer == null && TimeSinceUpdate > 5 )
		{
			List<Player> players = LethalGameManager.Instance.AlivePlayers.ToList();

			FocusedPlayer = null;

			// Find and focus closest player
			float closest = AggroDistance;
			foreach ( Player player in players )
			{
				float dst = Vector3.DistanceBetweenSquared( Transform.Position, player.Transform.Position );

				if ( dst < closest )
				{
					closest = dst;
					FocusedPlayer = player;
				}
			}

			TimeSinceUpdate = 0;

		}
		else if ( WasHidden && TimeSinceUpdate > 1 )
		{
			TimeSinceUpdate = 0;
			GleamAtFocusedPlayer();
		}

	}

	private void DoHunting( ref float speed, ref float turnSpeed, ref float friction )
	{
		return;
		UpdateIsSeen();
		Log.Info( IsSeen );

		if ( !IsSeen )
		{

			if ( TimeSinceUpdate > 4 )
			{
				Log.Info( "meep" );
				TimeSinceUpdate = 0;
				TargetPosition = Scene.NavMesh.GetRandomPoint( Transform.Position, 500 ) ?? Transform.Position;

				Agent.MoveTo( TargetPosition );
			}

			WantedMoveDirection = (Agent.GetLookAhead( 1 ) - Transform.Position).Normal;

			Controller.ApplyFriction( 100 );

			//float angle = Vector3.VectorAngle( WantedMoveDirection ).yaw;
			//EyeAngles = Rotation.Lerp( EyeAngles, new Angles( 0, angle, 0 ), Time.Delta * turnSpeed );
			//Controller.Accelerate( EyeAngles.Forward * speed );
			//Controller.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;

			if(TimeSinceMove > 0.25f)
			{
				TimeSinceMove = 0;
				Controller.Accelerate( EyeAngles.Forward * speed );
				Controller.MoveTo ( Transform.Position + WantedMoveDirection * 14, true );

				EyeAngles = Vector3.VectorAngle( WantedMoveDirection ).WithPitch( 0 ).WithRoll( 0 );
				Transform.Rotation = EyeAngles;

				if(!IsProxy) { UpdateAggroModel(); }

			}

		}

	}

	private void UpdateAggroModel()
	{
		UpdateAggroModelLocal( LethalGameManager.Random.Int( 3, 6 ) );
	}

	[Broadcast]
	private void UpdateAggroModelLocal(int state)
	{
		Renderer.Model = ModelStates[state];
	}


	private void DoAggro( ref float speed, ref float turnSpeed, ref float friction )
	{
		//speed = RunSpeed;
		//friction = 6;
		//turnSpeed = 15;

		//PhysicsCollider.Enabled = true;

		//Player playerToAggro = FindPlayerToAggro( out float dstToPlayerSqr );

		//if ( TimeSinceUpdateTarget > 0.1f )
		//{
		//	if ( playerToAggro != null )
		//	{
		//		AggroPlayer( playerToAggro );
		//	}
		//	else
		//	{
		//		// Stop aggroing.
		//		Log.Info( $"{GameObject.Name} stop aggroing" );
		//		StartPatroling( NestPosition );
		//	}
		//}

		//// Attack player
		//if ( playerToAggro != null && TimeSinceAttack > 2 && dstToPlayerSqr < AttackDistance )
		//{
		//	TimeSinceAttack = 0;
		//	Log.Info( $"Zombie attacked {playerToAggro.GameObject.Name}" );
		//	Animator?.TriggerDeploy();
		//	Controller.Punch( Vector3.Up * 150 );
		//	playerToAggro.Components.Get<IKillable>().TakeDamage( 40, GameObject.Id, Transform.Rotation.Up * 300 );
		//}

		//WantedMoveDirection = (Agent.GetLookAhead( 1 ) - Transform.Position).Normal;

		//if ( dstToPlayerSqr < AttackDistance )
		//{
		//	speed = 0.1f;
		//}

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

		//Health = MathF.Max( Health - damage, 0f );

		if ( Health <= 0f )
		{
			Tags.Add( "dead" );
			LifeState = LifeState.Dead;
			Agent.Enabled = false;
			TimeSinceDeath = 0;
		}
	}
}
