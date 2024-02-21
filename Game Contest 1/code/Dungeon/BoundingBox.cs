using Sandbox;

namespace Dungeon;

public sealed class BoundingBox : Component
{
	[Property] public BBox Bounds { get; set; }

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		Gizmo.Draw.Color = Color.Red;
		Gizmo.Draw.LineBBox( Bounds );

	}

}
