using Dungeon;
using Sandbox;

public sealed class LethalGame : Component
{
	public static LethalGame Instance { get; private set; }

	[Property] public DungeonDefinition DungeonDefinition { get; set; }

	public static Random Random { get; private set; }

	protected override void OnStart()
	{
		if(GameObject.IsProxy) { return; }

		if(Instance == null) { Instance = this; }
		else if(Instance != this) { this.Destroy(); }


		Random = new Random( );
		DungeonGenerator.GenerateDungeon( DungeonDefinition );
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

	}
}
