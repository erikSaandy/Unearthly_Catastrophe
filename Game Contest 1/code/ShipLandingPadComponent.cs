using Sandbox;

public sealed class ShipLandingPadComponent : Component
{
	protected override void OnUpdate()
	{

	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		Gizmo.Draw.Model( "models/spaceship/spaceship.vmdl" );

	}

}
