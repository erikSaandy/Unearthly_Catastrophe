using Sandbox;
using Sandbox.Citizen;
using System.Numerics;

public sealed class Flashlight : Carriable
{
	public override string ToolTip { get; set; } = "Pickup Flashlight";

	[Property] public SpotLight LightSource { get; set; }
	bool IsOn = false;

	[Property] public Vector3 ShoulderedOffset { get; set; }
	[Property] public Angles ShoulderedAngleOffset { get; set; }
	protected override void OnAwake()
	{
		base.OnAwake();
		GameObject.BreakFromPrefab();
		ToggleOn( false );
	}

	public override void OnInteract( Player player )
	{
		base.OnInteract( player );

		GameObject.Enabled = true;

	}

	public override void OnDrop()
	{
		base.OnDrop();
	}

	public override void OnUsePrimary()
	{
		ToggleOn( !IsOn );
	}

	public override void OnUseSecondary()
	{
	}


	public override void Undeploy()
	{
		base.Undeploy();

		GameObject.Enabled = true;

		if ( GameObject.IsProxy ) { return; }

		Owner.CurrentHoldType = CitizenAnimationHelper.HoldTypes.None;

		GameObject.SetParent( Owner.FlashlightRBone );
		GameObject.Transform.LocalPosition = 0;
		GameObject.Transform.LocalRotation = Quaternion.Identity;

		//Owner.CurrentHoldType = CitizenAnimationHelper.HoldTypes.None;

	}

	/// <summary>
	/// Toggle flashlight on or off.
	/// </summary>
	/// <param name="toggle"></param>
	[Broadcast]
	private void ToggleOn(bool toggle)
	{
		Components.Get<ModelRenderer>().MaterialGroup = toggle ? "default" : "off";
		LightSource.Enabled = toggle;
		IsOn = toggle;
	}
}
