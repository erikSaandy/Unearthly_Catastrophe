using Sandbox;
using Sandbox.Internal;
using System.Runtime.CompilerServices;

[Icon( "accessibility_new" )]
public sealed class MonsterSpawner : Component
{

	protected override void DrawGizmos()
	{

		base.DrawGizmos();

		Gizmo.Draw.Color = Color.Red.WithAlpha(0.7f);
		Model model = Model.Load( "models/editor/spawnpoint.vmdl" );
		Gizmo.Hitbox.Model( model );
		SceneObject sceneObject = Gizmo.Draw.Model( model );
		if ( sceneObject != null )
		{
			sceneObject.Flags.CastShadows = true;
		}
	}

}
