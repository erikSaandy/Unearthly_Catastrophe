using Sandbox;

public sealed class Moon : Component
{
	[Property] public MoonDefinition Definition { get; set; }
	[Property] public ShipLandingPadComponent LandingPad { get; set; }

}
