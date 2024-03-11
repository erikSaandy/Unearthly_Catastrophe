using Saandy;
using Sandbox;

public class ShipDoorComponent : Component
{


	[Sync] public bool IsOpen { get; private set; } = false;

	[Property][Sync] public bool IsLocked { get; set; } = false;

	[Category( "Open" )][Property] public Curve OpenCurve { get; set; }
	[Category( "Close" )][Property] public Curve CloseCurve { get; set; }

	[Property] public GameObject DoorLeft { get; set; }
	[Property] public GameObject DoorRight { get; set; }

	[Category( "Open" )][Property] public InteractionProxy ButtonOn { get; set; }
	[Category( "Close" )][Property] public InteractionProxy ButtonOff { get; set; }

	private float ReopenTime { get; set; } = 25f;
	private TimeSince TimeSinceReopen { get; set; } = 0;

	[Category( "Open" )] private float OpenedDistance { get; set; } = 96;
	[Category( "Open" )] private float OpenedScale { get; set; } = 0.5f;

	[Category( "Close" )] private float ClosedDistance { get; set; } = 68;
	[Category( "Close" )] private float ClosedScale { get; set; } = 1f;

	[Category("Open")][Property] public float OpenSpeed { get; set; } = 1f;
	[Category( "Close" )][Property] public float CloseSpeed { get; set; } = 1f;

	[Property][Category( "Sound" )] public SoundEvent OpenSound { get; set; }
	[Property][Category( "Sound" )] public SoundEvent CloseSound { get; set; }

	protected override void OnAwake() { base.OnAwake(); }

	protected override void OnStart()
	{
		base.OnStart();

		ButtonOn.OnInteracted += Open;
		ButtonOff.OnInteracted += Close;

		if (IsProxy) { return; }

		Lock();

	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if(!IsLocked && !IsOpen)
		{
			if(TimeSinceReopen >= ReopenTime)
			{
				Open();
			}
		}
		else
		{
			TimeSinceReopen = 0;
		}

	}

	private void Open( Player player )
	{
		Open();
	}
	
	[Broadcast]
	public void Open()
	{
		if ( IsOpen || IsLocked ) { return; }

		TimeSinceReopen = 0;
		IsOpen = true;

		Sound.Play( OpenSound, Transform.Position );
		OpenCloseAsync( ClosedScale, OpenedScale, new Vector3( ClosedDistance, 0, 0 ), new Vector3( OpenedDistance, 0, 0 ) );

	}


	[Broadcast]
	public void Lock()
	{
		Close();
		ButtonOn.BlockInteractions = true;
		ButtonOff.BlockInteractions = true;
		IsLocked = true;
	}

	[Broadcast]
	public void Unlock()
	{
		ButtonOn.BlockInteractions = false;
		ButtonOff.BlockInteractions = false;
		IsLocked = false;
		Open();
	}

	private void Close( Player player )
	{
		Close();
	}

	[Broadcast]
	public void Close()
	{
		if ( !IsOpen || IsLocked ) { return; }

		IsOpen = false;
		TimeSinceReopen = 0;

		Sound.Play( CloseSound, Transform.Position );
		OpenCloseAsync( OpenedScale, ClosedScale, new Vector3( OpenedDistance, 0, 0 ), new Vector3( ClosedDistance, 0, 0 ) );
	}

	private async void OpenCloseAsync( float currentScale, float targetScale, Vector3 currentPosition, Vector3 targetPosition )
	{
		bool openState = IsOpen;

		float t = 0;

		do
		{
			t += Time.Delta * (openState ? OpenSpeed : CloseSpeed);

			float eval = openState ? OpenCurve.Evaluate( t ) : CloseCurve.Evaluate( t );

			Vector3 pos = Vector3.Lerp( currentPosition, targetPosition, eval );
			float scale = MathX.Lerp( currentScale, targetScale, eval );

			await Task.DelayRealtime( 3 );

			ApplyTransforms( scale, pos );


		} while ( t < 1f && openState == IsOpen );

		ApplyTransforms( targetScale, targetPosition );

	}

	private void ApplyTransforms(float scale, Vector3 position)
	{
		DoorLeft.Transform.LocalPosition = position;
		DoorLeft.Transform.LocalScale = new Vector3( scale, 1, 1 );

		DoorRight.Transform.LocalPosition = -position;
		DoorRight.Transform.LocalScale = new Vector3( scale, 1, 1 );
	}

}
