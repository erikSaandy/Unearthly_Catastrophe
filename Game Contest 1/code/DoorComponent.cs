using Saandy;
using Sandbox;
using static Sandbox.Gizmo;
using System;

public class DoorComponent : Component, IInteractable
{
	public bool IsInteractableBy( Player player ) { return true; }// !IsLocked || IsOpen || (IsLocked && player.Inventory?.ActiveItem is Key); }
	public float InteractionTime { get { return IsLocked ? 0 : 0.7f; } }
	public string ToolTip { get; set; } = "";

	[Category("Sound")][Property] public SoundEvent OpenSound { get; set; }
	[Category( "Sound" )][Property] public SoundEvent CloseSound { get; set; }

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

	[Property] public float Speed { get; set; } = 1f;

	protected override void OnAwake()
	{
		base.OnAwake();

		RotationalAxis = RotationalAxis.Normal;
	}

	public void OnInteract( Guid playerId )
	{
		Player player = GameObject.Scene.Directory.FindByGuid( playerId )?.Components.Get<Player>();

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

		if(IsOpen) { Close(); }
		else { Open(); }
	}

	[Broadcast]
	public void Open()
	{
		if(IsOpen) { return; }
		Tags.Add( "open_door" );

		LerpAngles( OpenAngle );
	}

	[Broadcast]
	public void Close()
	{
		if ( !IsOpen ) { return; }
		Tags.Remove( "open_door" );

		LerpAngles( 0 );
	}

	private void LerpAngles( float targetAngle )
	{
		if(IsOpen)
		{
			Sound.Play( CloseSound, Transform.Position );
		}
		else
		{
			Sound.Play( OpenSound, Transform.Position );
		}

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
			t += Time.Delta * Speed;
			angle = Math2d.Lerp( angle, targetAngle, t );
			//Log.Info( angle );

			await Task.Delay( 5 );

			Transform.LocalRotation = new Angles( (RotationalAxis * angle));

		} while ( t < 0.95f && openState == IsOpen );
		
		Transform.LocalRotation = new Angles( (RotationalAxis * angle) );

	}
}
