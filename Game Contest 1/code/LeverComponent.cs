using Saandy;
using Sandbox;

public class LeverComponent : Component, IInteractable
{

	public bool IsInteractableBy( Player player ) { return !IsLocked; }
	public float InteractionTime { get; set; } = 1.3f;
	[Category("ToolTips")][Property] public string ToolTip { get; set; } = "";
	[Category( "ToolTips" )][Property] public string ToolTipDeactivated { get; set; } = "";

	[Property][Category( "Sound" )] public SoundEvent PullSound { get; set; }

	public string GetToolTip( Player player )
	{

		if ( IsLocked )
		{
			return "";
		}
		else if ( IsActivated )
		{
			return $"{IInteractable.GetInteractionKey()} - {ToolTip}";
		}
		else
		{
			return $"{IInteractable.GetInteractionKey()} - {ToolTipDeactivated}";
		}

	}

	[Property] public Action OnActivate { get; set; }
	[Property] public Action OnDeactivate { get; set; }

	[Property] public bool StartActivated { get; private set; } = false;
	public bool IsActivated { get; private set; } = false;

	[Property][Sync] public bool IsLocked { get; set; } = false;

	[Property] public float DeactivatedAngle { get; set; } = 1f;
	[Property] public float ActivatedAngle { get; set; } = 90f;
	[Property] public Vector3 RotationalAxis { get; set; } = new Vector3( 1, 0, 0 );

	[Property] public float Speed { get; set; } = 1f;

	protected override void OnStart()
	{
		base.OnStart();

		if(StartActivated) { Activate(); }

		OnActivate += PlaySound;
		OnDeactivate += PlaySound;

	}

	protected override void OnUpdate()
	{

	}

	public void OnInteract( Guid playerId )
	{
		Player player = GameObject.Scene.Directory.FindByGuid( playerId )?.Components.Get<Player>();
		ToggleState();
	}

	public void ToggleState()
	{
		if ( IsActivated ) { 
			Deactivate(); 
		}
		else { 
			Activate(); 
		}
	}

	[Broadcast]
	public void Deactivate(bool invokeAction = true)
	{
		LerpAngles( false, DeactivatedAngle );

		if ( invokeAction )
			OnDeactivate?.Invoke();
	}

	[Broadcast]
	public void Activate( bool invokeAction = true )
	{
		LerpAngles( true, ActivatedAngle );


		if (invokeAction)
			OnActivate?.Invoke();
	}

	[Broadcast]
	private void LerpAngles( bool activeState, float targetAngle )
	{
		LerpAnglesAsync( activeState, targetAngle );
	}

	private async void LerpAnglesAsync( bool activeState, float targetAngle )
	{
		IsActivated = activeState;

		float angle = (Transform.LocalRotation.Angles().AsVector3() * RotationalAxis).Length; ;
		float t = 0;

		do
		{
			t += Time.Delta * Speed;
			angle = Math2d.LerpAngle( angle, targetAngle, t );
			//Log.Info( angle );

			await Task.Delay( 5 );

			Transform.LocalRotation = new Angles( (RotationalAxis * angle) );

		} while ( t < 0.95f && activeState == IsActivated );


		Transform.LocalRotation = new Angles( (RotationalAxis * targetAngle) );

	}

	private void PlaySound()
	{
		Sound.Play( PullSound, Transform.Position );
	}

}
