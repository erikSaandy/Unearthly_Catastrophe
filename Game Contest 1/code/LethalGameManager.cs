using Dungeon;
using Sandbox;

public sealed class LethalGameManager : Component
{
	public static LethalGameManager Instance { get; private set; }

	[Property] public List<DungeonDefinition> DungeonDefinitions { get; set; }

	[Property] public List<MoonDefinition> MoonDefinitions { get; set; }

	[Property] public MapInstance MapInstance { get; set; }


	/// <summary>
	/// How much money does the team have?
	/// </summary>
	public static int Credits { get; set; }

	public static Random Random { get; private set; }

	protected override void OnStart()
	{
		if(GameObject.IsProxy) { return; }

		if(Instance == null) { Instance = this; }
		else if(Instance != this) { this.Destroy(); }

		Random = new Random( );
		DungeonGenerator.GenerateDungeon( DungeonDefinitions[0] );
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if(Input.Pressed("space"))
		{
			MapInstance.MapName = MoonDefinitions[0].MapPath;
		}
	}
}
