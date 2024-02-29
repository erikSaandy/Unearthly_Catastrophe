using Saandy;
using Sandbox;

public class DoorComponent : Component, IInteractable
{
	public bool IsInteractable( Player player ) { return !IsLocked || IsOpen || (IsLocked && player.Inventory?.ActiveItem is Key); }
	public float InteractionTime { get; set; } = 0.7f;
	public string ToolTip { get; set; } = "";


	public virtual string GetToolTip( Player player ) { 

		if( IsLocked )
		{
			if(player.Inventory?.ActiveItem is Key)
			{
				return $"{IInteractable.GetInteractionKey()} - Unlock Door";
			}
			else
			{
				return "[ Locked ]";
			}
		} 
		else if(IsOpen)
		{
			return $"{IInteractable.GetInteractionKey()} - Close Door";
		}
		else
		{
			return $"{IInteractable.GetInteractionKey()} - Open Door";
		}


	}


	public bool IsOpen { get; private set; } = false;
	[Property][Sync] public bool IsLocked { get; set; } = false;


	[Property] public float OpenAngle { get; set; } = -135f;
	[Property] public Vector3 RotationalAxis { get; set; } = new Vector3( 0, 1, 0 );

	protected override void OnAwake()
	{
		base.OnAwake();

		RotationalAxis = RotationalAxis.Normal;
	}

	public void OnInteract( Player player )
	{
		if ( IsLocked )
		{
			if ( player.Inventory?.ActiveItem is Key )
			{
				IsLocked = false;
			}
			else
			{
				return;
			}
		}

		if(IsOpen) { LerpAngles( 0 ); }
		else { LerpAngles( OpenAngle ); }
	}

	[Broadcast]
	private void LerpAngles( float targetAngle )
	{
		LerpAnglesAsync( targetAngle );
	}

	private async void LerpAnglesAsync(float targetAngle)
	{
		IsOpen = !IsOpen;
		bool openState = IsOpen;

		float angleMag = (Transform.LocalRotation.Angles().AsVector3() * RotationalAxis).Length;
		int dir = MathF.Sign( targetAngle - angleMag );
		float angle = angleMag * dir;
		float t = 0;

		do
		{
			t += Time.Delta * 4;
			angle = Math2d.Lerp( angle, targetAngle, t );
			//Log.Info( angle );

			await Task.Delay( 20 );

			Transform.LocalRotation = new Angles( (RotationalAxis * angle));

		} while ( t < 0.95f && openState == IsOpen );
		
		Transform.LocalRotation = new Angles( (RotationalAxis * angle) );

	}
}
