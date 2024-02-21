using Sandbox;
using Sandbox.Citizen;
using System.Numerics;

public sealed class Flashlight : Carriable
{

	[Property] public SpotLight LightSource { get; set; }
	bool IsOn = false;

	[Property] public Vector3 ShoulderedOffset { get; set; }
	[Property] public Angles ShoulderedAngleOffset { get; set; }
	protected override void OnAwake()
	{
		base.OnAwake();
		ToggleOn( false );
	}

	public override void OnPickup( Player player )
	{
		base.OnPickup( player );
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
		base.Deploy();

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
