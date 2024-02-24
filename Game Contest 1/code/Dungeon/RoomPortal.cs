using Sandbox;
using System.Text.Json.Serialization;

namespace Dungeon;

public sealed class RoomPortal : Component
{
	public enum RoomPortalType
	{
		Door,
		Corridor,
		Entrance
	}


	[JsonIgnore]
	private static Vector3[] portalSizes = new Vector3[]
	{
		new Vector3( 56, 8, 96 ),
		new Vector3( 128, 8, 128 ),
		new Vector3( 96, 8, 96 )
	};

	[JsonIgnore] public Vector3 Size => portalSizes[(int)PortalType];

	[Property] public RoomPortalType PortalType { get; set; }

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		Gizmo.Draw.Color = Color.Blue.WithAlpha(0.5F);
		Gizmo.Draw.SolidBox(GetBBox());

	}

	private BBox GetBBox()
	{
		return new BBox(
				new Vector3( -Size.x * 0.5f, -Size.y * 0.5f, 0 ),
				new Vector3( Size.x * 0.5f, Size.y * 0.5f, Size.z ) );


	}

}
