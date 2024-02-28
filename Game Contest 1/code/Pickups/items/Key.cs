using Sandbox;
using Sandbox.Citizen;
using System.Numerics;

public sealed class Key : Carriable
{
	public override string ToolTip { get; set; } = "Pickup Key";

	protected override void OnAwake() { base.OnAwake(); }

	public override void OnInteract( Player player )
	{
		base.OnInteract( player );

		GameObject.Enabled = true;

	}

	public override void OnDrop() { base.OnDrop(); }

	public override void OnUsePrimary()	{ }
	public override void OnUseSecondary() { }


	public override void Deploy() { base.Deploy(); }

	public override void Undeploy() { base.Undeploy(); }

}
