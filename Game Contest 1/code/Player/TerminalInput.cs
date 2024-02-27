using Sandbox;
using Sandbox.UI;
using Sandbox.VR;
using System;

public class TerminalInput : PlayerInput
{
	private TerminalComponent Terminal { get; set; }

	public TerminalInput( Player owner, TerminalComponent terminal ) : base(owner) {

		this.Terminal = terminal;

		Owner.Camera.GameObject.SetParent( Terminal.GameObject );
	}

	public override void UpdateInput()
	{
		AnalogMove = 0;
		WantsToRun = false;

	}

	public override void OnJump() { 	}

	public override void CameraInput()
	{
		Owner.Camera.Transform.LocalRotation = Terminal.CameraAngles;
		Owner.Camera.Transform.LocalPosition = Terminal.CameraPosition;

	}

	public override void InventoryInput()
	{
		base.InventoryInput();
	}

}
