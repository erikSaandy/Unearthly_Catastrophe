using Sandbox;
using Sandbox.UI;
using Sandbox.VR;
using System;

public class PlayerTerminalInput : PlayerInput
{
	private TerminalComponent Terminal { get; set; }

	public PlayerTerminalInput( Player owner, TerminalComponent terminal ) : base(owner) {

		this.Terminal = terminal;

		owner.Voice.Volume = 1;
		//Owner.Camera.GameObject.SetParent( Terminal.GameObject );
	}

	public override void UpdateInput()
	{
		Owner.WishVelocity = 0;
		AnalogMove = 0;
		WantsToRun = false;

	}

	public override void OnJump() { 	}

	public override void CameraInput()
	{

	}

	public override void InventoryInput()
	{
		base.InventoryInput();
	}

	public override void OnPreRender()
	{
		Owner.Camera.Transform.Rotation = Terminal.CameraAngles;
		Owner.Camera.Transform.Position = Terminal.ScreenCollider.Transform.Position + Terminal.ScreenCollider.Transform.Rotation.Up * Terminal.ScreenDistance;
	}

}
