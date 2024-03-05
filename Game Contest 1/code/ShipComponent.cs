using Saandy;
using Sandbox;
using System.Diagnostics;
using System.Threading.Tasks;

public sealed class ShipComponent : Component
{
	private readonly Vector3 SPACE_POSITION = new Vector3( 0, -5000, -3500 );

	[Property] public Curve DockingCurveXY { get; set; }
	[Property] public Curve DockingCurveAcceleration { get; set; }

	[Property] public float DockingTime { get; set; } = 6f;

	[Category( "Components" )][Property] private ShipLandingPadComponent CurrentLandingPad { get; set; }

	[Sync] private Vector3 WantedPosition { get; set; }
	[Sync] private bool IsMoving { get; set; } = false;

	public PassengerTransporter Transporter { get; private set; }

	[Category("Components")][Property] public LeverComponent Lever { get; set; }

	[Category( "Components" )][Property] public ShipDoorComponent Doors { get; set; }

	protected override void OnAwake()	
	{
		base.OnAwake();

		GameObject.BreakFromPrefab();

		WantedPosition = SPACE_POSITION;

		Transporter = GameObject.Components.GetInChildren<PassengerTransporter>();
		//OnMoveShip += Transporter.MovePassengers;

		Lever.IsLocked = true;
		Lever.OnActivate += LethalGameManager.Instance.LoadSelectedMoon;
		Lever.OnDeactivate += LethalGameManager.Instance.LeaveCurrentMoon;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if(IsProxy) { return; }

		if(IsMoving)
		{
			if ( Vector3.DistanceBetween( WantedPosition, Transform.Position ) > 8f )
			{
				foreach ( CharacterController passenger in Transporter.Passengers )
				{
					passenger.GameObject.SetParent( GameObject );
				}

				Vector3 oldPos = Transform.Position;
				Transform.Position = Math2d.Lerp( Transform.Position, WantedPosition, Time.Delta * Time.Delta * 20 );
				Vector3 newPos = Transform.Position;
				Vector3 deltaPos = newPos - oldPos;

				foreach ( CharacterController passenger in Transporter.Passengers )
				{
					passenger.GameObject.SetParent( Scene );
				}

			}
			else
			{
				StopMoving();
			}

		}

	}

	private void StartMovingTo(Vector3 pos)
	{
		Lever.IsLocked = true;
		IsMoving = true;
		WantedPosition = pos;
	}

	private void StopMoving()
	{
		IsMoving = false;
		Lever.IsLocked = false;
	}

	public void Land(ShipLandingPadComponent landingPad)
	{
		CurrentLandingPad = landingPad;
		//Transform.Rotation = CurrentLandingPad.Transform.Rotation;

		WantedPosition = CurrentLandingPad.Transform.Position;

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

	public void TakeOff()
	{
		StartMovingTo( SPACE_POSITION );
		CurrentLandingPad = null;
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
