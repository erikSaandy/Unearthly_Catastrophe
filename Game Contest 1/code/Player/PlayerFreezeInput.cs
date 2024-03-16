using Sandbox;
using Sandbox.UI;
using Sandbox.VR;
using System;

public class PlayerFreezeInput : PlayerInput
{

	public PlayerFreezeInput( Player owner ) : base( owner ) {
		owner.Voice.Volume = 1;
	}

	public override void UpdateInput()
	{
		Owner.WishVelocity = 0;
		AnalogMove = 0;
		WantsToRun = false;

		if ( Owner.IsProxy ) { return; }

		Owner.Controller.Velocity = 0;

		if ( Sandbox.Input.Pressed( "mute" ) )
		{
			ToggleMicrophone();
		}

	}

	public override void OnJump() { }

	public override void CameraInput() { }

	public override void InventoryInput() { }

	public override void OnPreRender() { }

}
