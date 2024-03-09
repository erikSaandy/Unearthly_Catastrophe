using Saandy;
using Sandbox;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using static Sandbox.PhysicsContact;

public sealed class ShipComponent : Component
{

	private readonly Vector3 SPACE_POSITION = new Vector3( 0, -3000, 1500 );

	private CharacterController Controller { get; set; }

	[Property] public Curve DockingCurveXY { get; set; }
	[Property] public Curve DockingCurveAcceleration { get; set; }

	[Category( "Components" )][Property] private ShipLandingPadComponent CurrentLandingPad { get; set; }

	[Sync][Property] public float Speed { get; set; }
	private float SpeedPrev { get; set; }
	private readonly float SpeedMin = 20;
	private readonly float SpeedMax = 250;
	private float accelerationFactor = 300.0f;
	private float slowDownRadius = 250;

	private Vector3 Velocity { get; set; } = new();

	[Sync] private bool IsMoving { get; set; } = false;


	[Category( "Components" )][Property] public PassengerTransporter Transporter { get; private set; }

	[Category("Components")][Property] public LeverComponent Lever { get; set; }

	[Category( "Components" )][Property] public ShipDoorComponent Doors { get; set; }

	private Vector3 TargetPosition { get; set; }

	protected override void OnAwake()	
	{
		base.OnAwake();

		GameObject.BreakFromPrefab();

		Controller = GameObject.Components.Get<CharacterController>();
		//OnMoveShip += Transporter.MovePassengers;

		Lever.IsLocked = true;
		Lever.OnActivate += LethalGameManager.Instance.LoadSelectedMoon;
		Lever.OnDeactivate += LethalGameManager.Instance.LeaveCurrentMoon;

		TargetPosition = SPACE_POSITION;


	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if(IsProxy) { return; }



		BuildVelocity();

		if ( IsMoving )
		{
			Move( Velocity );
		}



	}

	private void BuildVelocity()
	{
		var distanceToTarget = Vector2.Distance( GameObject.Transform.Position, TargetPosition );

		if ( distanceToTarget <= 4f )
		{

			StopMoving();

			// do nothing else 
			return;
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
		Velocity = dir * Speed;

	}

	[Broadcast]
	private void Move( Vector3 velocity )
	{

		if(!IsProxy)
		{
			Controller.Velocity = velocity;
			Controller.Move();
		}


		foreach ( Player player in Transporter?.Passengers )
		{

			if(!player.IsProxy)
			{	
				player.Controller.MoveTo( player.Transform.Position + (velocity / Time.Delta), true );
				break;
			}

		}

	}

	private void StartMovingTo(Vector3 pos)
	{
		Lever.IsLocked = true;
		IsMoving = true;
		TargetPosition = pos;
	}

	private void StopMoving()
	{
		Speed = 0;
		Velocity = 0;
		IsMoving = false;
		Lever.IsLocked = false;
	}

	public void Land(ShipLandingPadComponent landingPad)
	{
		CurrentLandingPad = landingPad;
		//Transform.Rotation = CurrentLandingPad.Transform.Rotation;
		//LandAsync( landingPad.Transform.Position );

		Doors.Unlock();
		StartMovingTo( CurrentLandingPad.Transform.Position );

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

	public async Task FlyIntoSpaceLol()
	{
		Lever.Deactivate( invokeAction: false );

		StartMovingTo( SPACE_POSITION );
		CurrentLandingPad = null;

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
