using Sandbox;

namespace Dungeon;

public sealed class RoomData : Component
{
	[Property] public List<RoomPortal> Portals { get; private set; }
	[Property] public ModelRenderer Renderer { get; set; }

	[Property] public BBox Bounds { get; set; }

	protected override void DrawGizmos()
	{
		Gizmo.Draw.Color = Color.Red;
		Gizmo.Draw.LineBBox( Bounds );

		base.DrawGizmos();
	}


}
