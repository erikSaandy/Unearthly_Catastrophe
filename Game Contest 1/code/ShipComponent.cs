using Saandy;
using Sandbox;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;

public sealed class ShipComponent : Component
{
	public enum MovementState
	{
		Docked,
		Leaving,
		Landing
	}

	[Sync] public MovementState CurrentMovementState { get; set; } = MovementState.Docked;

	private readonly Vector3 SPACE_POSITION = new Vector3( 0, -3000, 1500 );

	public CharacterController Controller { get; private set; }

	[Category( "Components" )][Property] private ShipLandingPadComponent CurrentLandingPad { get; set; }

	[Sync] public float Speed { get; set; }
	[Sync] float SpeedPrev { get; set; }
	private readonly float SpeedMin = 20;
	private readonly float SpeedMax = 250;
	private float accelerationFactor = 300.0f;
	private float slowDownRadius = 250;

	[Sync] public bool IsMoving { get; private set; } = false;


	[Category( "Components" )][Property] public PassengerTransporter Transporter { get; private set; }

	[Category("Components")][Property] public LeverComponent Lever { get; set; }

	[Category( "Components" )][Property] public ShipDoorComponent Doors { get; set; }

	[Sync] private Vector3 TargetPosition { get; set; }

	[Category( "Sounds" )][Property] List<SoundPointComponent> Thrusters { get; set; }
	[Property][Range(0, 1)] float ThrusterVolume { get; set; } = 0.4f;

	protected override void OnStart()	
	{
		base.OnAwake();

		GameObject.BreakFromPrefab();

		Controller = GameObject.Components.Get<CharacterController>();
		TargetPosition = SPACE_POSITION;

		//Lever.IsLocked = true;
		Lever.OnActivate += LethalGameManager.Instance.LoadSelectedMoon;
		Lever.OnDeactivate += LethalGameManager.Instance.LeaveCurrentMoon;

	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		Lever.OnActivate -= LethalGameManager.Instance.LoadSelectedMoon;
		Lever.OnDeactivate -= LethalGameManager.Instance.LeaveCurrentMoon;

	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( IsMoving )
		{
			Move();
		}

	}

	[Broadcast]
	private void StartThrusters()
	{
		ToggleThrustersAsync( true );
	}


	[Broadcast]
	private void StopThrusters()
	{
		ToggleThrustersAsync( false, speed: 3 );
	}

	private async void ToggleThrustersAsync(bool activate = true, float speed = 1)
	{

		foreach (SoundPointComponent point in Thrusters)
		{
			point.Volume = activate ? 0 : 1;
			point.StartSound();
		}


		// toggle thrusters gradually
		float t = 0;
		float volume = 0;

		do
		{
			t += Time.Delta * 0.01f * speed;
			volume = (activate ? t : (t * -1) + 1) * ThrusterVolume;

			for ( int i = 0; i < Thrusters.Count; i++ )
			{
				Thrusters[i].Volume = volume;
			}

			await Task.Yield();

		}
		while ( t < 1f );



	}

	private void Move()
	{

		Vector3 velocity = BuildVelocity();

		foreach ( Player player in Transporter?.Passengers )
		{

			// Idfk
			if(player.IsProxy) { continue; }
			if ( !player.IsValid() ) { continue; }
			if (player.LifeState == LifeState.Dead) { continue; }
			if(player.Controller == null) { continue; }

			//Log.Info( player.GameObject.Name + " move on ship" );

			//Vector3 tVel = player.Controller.Velocity;
			player.Controller?.MoveTo( player.Transform.Position + (velocity * Time.Delta), true );
			//player.Controller.Velocity = velocity;
			//player.Controller.Move();
			//player.Controller.Velocity = tVel;

			// Snap to ship
			Vector3 from = player.Transform.Position + Vector3.Zero.WithZ( player.Controller.Height * 0.5f );
			Vector3 to = player.Transform.Position - Vector3.Zero.WithZ( player.Controller.Height * 0.5f );
			SceneTraceResult trace = Scene.Trace.Ray( from, to ).Radius( 2 ).IgnoreGameObjectHierarchy( player.GameObject ).WithoutTags( "item" ).Run();

			if ( trace.Hit )
			{
				player.Transform.Position = trace.EndPosition;
			}

		}

		if(IsProxy) { return; }

		Controller.Velocity = velocity;
		Controller.Move();

		//if ( !IsProxy )
		//{
		//	Controller.Velocity = ( velocity );
		//	Controller.Move();
		//}

	}

	private void StartMovingTo(Vector3 pos)
	{
		//Lever.IsLocked = true;
		IsMoving = true;
		TargetPosition = pos;
		StartThrusters();
	}

	private void StopMoving()
	{
		Speed = 0;
		Controller.Velocity = 0;
		IsMoving = false;
		//Lever.IsLocked = false;
		CurrentMovementState = MovementState.Docked;
		Transform.Position = TargetPosition;
		StopThrusters();
	}

	public void Land(ShipLandingPadComponent landingPad)
	{
		CurrentMovementState = MovementState.Landing;
		CurrentLandingPad = landingPad;
		//Transform.Rotation = CurrentLandingPad.Transform.Rotation;
		//LandAsync( landingPad.Transform.Position );

		Doors.Unlock();
		StartMovingTo( CurrentLandingPad.Transform.Position );
	}

	private Vector3 BuildVelocity()
	{
		var distanceToTarget = Vector2.Distance( GameObject.Transform.Position, TargetPosition );

		if ( distanceToTarget <= 4f )
		{

			StopMoving();

			// do nothing else 
			return 0;
		}

		IsMoving = true;

		if ( distanceToTarget <= 128 )
		{
			// decelerate
			// This will make it slower 
			// the closer we get to the target position
			Speed = SpeedPrev * (distanceToTarget / slowDownRadius);

			// as long as it is not in the final position
			// it should always keep a minimum speed
			Speed = Math.Max( Speed, SpeedMin );
		}
		else
		{
			// accelerate
			Speed += accelerationFactor * Time.Delta;

			// Limit to MaxVelocity
			Speed = Math.Min( Speed, SpeedMax );

			SpeedPrev = Speed;
		}

		Vector3 dir = (TargetPosition - GameObject.Transform.Position).Normal;
		return dir * Speed;

	}

	/*
	private async void LandAsync(Vector3 landingPadPosition)
	{
		float t = 0;
		float timeScale = 1f / DockingTime;

		Vector3 startPos = Transform.Position;
		Vector3 endPos = CurrentLandingPad.Transform.Position;

		if ( !Lever.IsActivated )
		{
			Lever.Activate();
		}

		await Task.Delay( 1000 );

		Doors.Unlock();

		do
		{
			t += Time.Delta * timeScale * DockingCurveAcceleration.Evaluate( t );
			//float xyEval = DockingCurveXY.Evaluate( 1-t );

			await Task.Delay( 1 );

			WantedPosition = Vector3.Lerp( startPos, endPos, t, true );

		} while ( t < 1f );

		await Task.Delay( 1000 );

		Lever.IsLocked = false;

		WantedPosition = endPos;

	}
	*/

	public async Task FlyIntoSpace()
	{
		CurrentMovementState = MovementState.Leaving;
		Lever.Deactivate( invokeAction: false );

		StartMovingTo( SPACE_POSITION );
		CurrentLandingPad = null;

		await Task.DelayRealtimeSeconds( 5 );

		LethalGameManager.Instance.KillAllStrandedPlayers();

		do
		{
			await Task.Yield();

		} while ( IsMoving );

	}

	/*
	private async Task TakeOffAsync()
	{
		float t = 0f;
		float timeScale = 1f / DockingTime;

		Vector3 endPos = SPACE_POSITION;
		Vector3 startPos = Transform.Position;

		if ( Lever.IsActivated )
		{
			Lever.Deactivate();
		}

		Lever.IsLocked = false;

		await Task.Delay( 1000 );

		do
		{
			t += Time.Delta * timeScale * DockingCurveAcceleration.Evaluate( t );
			float xyEval = DockingCurveXY.Evaluate( t );

			await Task.Delay( 5 );

			WantedPosition = Vector3.Lerp( startPos, endPos, t, true );

		} while ( t < 1f );

		Doors.Lock();

		await Task.Delay( 1000 );


		WantedPosition = endPos;

	}
	*/

}
