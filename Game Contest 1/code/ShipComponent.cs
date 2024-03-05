using Sandbox;
using System.Threading.Tasks;

public sealed class ShipComponent : Component
{
	private readonly Vector3 SPACE_POSITION = new Vector3( 2500, 0, 2500 );

	[Property] public Curve DockingCurveXY { get; set; }
	[Property] public Curve DockingCurveAcceleration { get; set; }

	[Property] public float DockingTime { get; set; } = 6f;

	[Property] private ShipLandingPadComponent CurrentLandingPad { get; set; }

	private Vector3 WantedPosition { get; set; }

	public Action<Vector3> OnMoveShip { get; set; }

	public PassengerTransporter Transporter { get; set; }

	[Property] public LeverComponent Lever { get; set; }

	protected override void OnAwake()	
	{
		base.OnAwake();

		GameObject.BreakFromPrefab();

		WantedPosition = SPACE_POSITION;

		//Transporter = GameObject.Components.GetInChildren<PassengerTransporter>();
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

		Vector3 deltaPos = WantedPosition - Transform.Position;

		if(deltaPos.Length > 0f)
		{
			OnMoveShip?.Invoke( deltaPos );
			Transform.Position += deltaPos;
		}

	}

	public void Land(ShipLandingPadComponent landingPad)
	{
		CurrentLandingPad = landingPad;
		Transform.Rotation = CurrentLandingPad.Transform.Rotation;

		LandAsync( landingPad.Transform.Position );
	}

	
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

		do
		{
			t += Time.Delta * timeScale * DockingCurveAcceleration.Evaluate( t );
			float xyEval = DockingCurveXY.Evaluate( t );

			await Task.Delay( 5 );

			WantedPosition = Vector3.Lerp( startPos, endPos, t, true );

		} while ( t < 1f );

		await Task.Delay( 1000 );

		Lever.IsLocked = false;

		WantedPosition = endPos;

	}

	public async Task TakeOff()
	{
		await TakeOffAsync();

		CurrentLandingPad = null;
	}

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

		await Task.Delay( 1000 );

		WantedPosition = endPos;

	}

}
