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

	public int ShopPrice { get; set; } = 20;

	[Property][Category( "Sound" )] public SoundEvent OnOffSound { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();
		GameObject.BreakFromPrefab();
		ToggleOn( false, playSound: false );
	}

	public override void OnInteract( Guid playerId )
	{

		base.OnInteract( playerId );

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
		//base.Undeploy();

		Renderer.Enabled = true;

		if ( GameObject.IsProxy ) { return; }

		if ( Owner.Inventory.ActiveItem == this )
		{
			Owner.CurrentHoldType = CitizenAnimationHelper.HoldTypes.None;
		}

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
	private void ToggleOn(bool toggle, bool playSound = true )
	{
		Components.Get<ModelRenderer>().MaterialGroup = toggle ? "default" : "off";
		LightSource.Enabled = toggle;
		IsOn = toggle;

		if ( playSound ) { Sound.Play( OnOffSound, Transform.Position ); }
	}
}
