using Sandbox;
using Sandbox.Citizen;
using System.Numerics;

public sealed class Key : Carriable
{
	public override string ToolTip { get; set; } = "Pickup Key";

	public int ShopPrice { get; set; } = 10;

	public override void OnUsePrimary()	{ }
	public override void OnUseSecondary() { }

}
