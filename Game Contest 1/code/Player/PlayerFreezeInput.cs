using Sandbox;
using Sandbox.UI;
using Sandbox.VR;
using System;

public class PlayerFreezeInput : PlayerInput
{

	public PlayerFreezeInput( Player owner ) : base( owner ) { }

	public override void UpdateInput()
	{
		AnalogMove = 0;
		WantsToRun = false;

		Owner.Controller.Velocity = 0;

	}

	public override void OnJump() { }

	public override void CameraInput() { }

	public override void InventoryInput() { }


}
