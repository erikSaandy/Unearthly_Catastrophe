using Sandbox;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Dungeon;

public sealed class RoomData : Component
{
	[Property] public List<RoomPortal> Portals { get; private set; }
	[Category("Components")][Property] public ModelRenderer Renderer { get; set; }
	[Category( "Components" )][Property] public LootSpawnerComponent LootSpawner { get; set; }

	[Property] public List<MonsterSpawner> MonsterSpawners { get; private set; }

	[Property] public BBox Bounds { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

	}

	protected override void DrawGizmos()
	{
		BBox AdjustedBounds = Bounds.Translate(-Transform.Position );
		AdjustedBounds = AdjustedBounds.Rotate( Rotation.Identity.Angles() - Transform.Rotation.Angles() );

		Gizmo.Draw.Color = Color.Orange;
		Gizmo.Draw.LineBBox( AdjustedBounds );

		base.DrawGizmos();
	}


}
