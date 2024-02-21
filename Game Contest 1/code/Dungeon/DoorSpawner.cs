using Sandbox;

namespace Dungeon;

public sealed class DoorSpawner : Component
{
	public enum DoorType
	{
		Default,
		MainDoor
	}

	readonly Vector3 DefaultDoorSize = new Vector3( 56, 8, 96 );
	readonly Vector3 MainDoorSize = new Vector3( 96, 8, 96 );

	[Property] DoorType Type { get; set; }

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		Gizmo.Draw.Color = Color.Blue.WithAlpha(0.5F);
		Gizmo.Draw.SolidBox(GetBBox());

	}

	private BBox GetBBox()
	{
		switch(Type)
		{
			case DoorType.MainDoor: return new BBox(
				new Vector3( -MainDoorSize.x * 0.5f, -MainDoorSize.y * 0.5f, 0 ),
				new Vector3( MainDoorSize.x * 0.5f, MainDoorSize.y * 0.5f, MainDoorSize.z ) );
			default:
				return new BBox(
				new Vector3( -DefaultDoorSize.x * 0.5f, -DefaultDoorSize.y * 0.5f, 0 ),
				new Vector3( DefaultDoorSize.x * 0.5f, DefaultDoorSize.y * 0.5f, DefaultDoorSize.z ) );
		}
	}

}
