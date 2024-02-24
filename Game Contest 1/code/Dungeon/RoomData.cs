using Sandbox;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Dungeon;

public sealed class RoomData : Component
{
	[Property] public List<RoomPortal> Portals { get; private set; }
	[Property] public ModelRenderer Renderer { get; set; }

	[Property] public BBox Bounds { get; set; }

	protected override void DrawGizmos()
	{
		BBox AdjustedBounds = Bounds.Translate(-Transform.Position );
		AdjustedBounds = AdjustedBounds.Rotate( Rotation.Identity.Angles() - Transform.Rotation.Angles() );

		Gizmo.Draw.Color = Color.Orange;
		Gizmo.Draw.LineBBox( AdjustedBounds );

		base.DrawGizmos();
	}


}
