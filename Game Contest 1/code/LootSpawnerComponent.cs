using Saandy;
using Sandbox;

public sealed class LootSpawnerComponent : Component
{
	[Property] public Vector3 Offset { get; set; } = Vector3.Zero;
	[Property] public float Radius { get; set; } = 64;
	[Property] public int MaxItemCount { get; set; } = 4;

	protected override void OnUpdate()
	{

	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		Gizmo.Draw.Color = new Color( 0, 1, 0.5f, 0.8f );
		Gizmo.Draw.SolidCylinder( Offset, Offset + Vector3.Up * 10, Radius, 32 );

	}

	public bool TrySpawnItem(string prefabPath)
	{
		PrefabFile pf = null;
		if ( !ResourceLibrary.TryGet<PrefabFile>( prefabPath, out pf ) ) { return false; }
		GameObject lootObj = SceneUtility.GetPrefabScene( pf ).Clone();
		lootObj.BreakFromPrefab();
		Scrap scrap = lootObj.Components.Get<Scrap>();

		// Try to place item five times.
		for (int i = 0; i < 5; i++ )
		{
			// Get position within radius
			float angle = (LethalGameManager.Random.Next( 0, 100 ) * 0.01f) * MathF.Tau;
			Vector3 dir = new Vector3( (float)Math.Sin( angle ), (float)Math.Cos( angle ), 0 );
			float dst = LethalGameManager.Random.Next( 0, 100 ) * 0.01f * Radius;
			scrap.Transform.Position = Transform.Position + Offset + (dir * dst);

			SceneTraceResult trace = scrap.DropToGround();

			float dot = Vector3.Dot( trace.Normal, Vector3.Up );

			// Placed item on ground! return true.
			if ( !trace.StartedSolid && trace.Hit && dot > 0.7f )
			{
				Log.Info( "placed scrap on ground." );
				scrap.GameObject.Transform.Rotation = Angles.Zero.WithYaw( LethalGameManager.Random.Next( 0, 360 ) );
				scrap.GameObject.NetworkSpawn();
				return true;
			}


		}

		// Scrap was unable to be placed on ground.

		Log.Info( ">> destroy scrap :(" );
		scrap.GameObject.Destroy();


		return true;
	}

}
