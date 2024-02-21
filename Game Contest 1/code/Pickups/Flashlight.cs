using Sandbox;

public sealed class Flashlight : Carriable
{

	[Property] public SpotLight LightSource { get; set; }
	bool IsOn = false;

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
