using System.Text.Json.Serialization;

[GameResource( "Moon Definition", "moon", "This asset defines a destination for players to travel to." )]
public partial class MoonDefinition : GameResource
{
	[JsonInclude][Property] public string Name { get; set; }
	[JsonInclude][Property] public string Description { get; set; }
	[JsonInclude][Property][Range(0, 2000)] public int TravelCost { get; set; }
	
	//TODO: Dungeon size stuff, loot potnetial, danger

	[ResourceType( "prefab" )][JsonInclude][Property] public string MoonPrefab { get; set; }


	[ResourceType( "dungeon" )][JsonInclude][Property] public List<string> DungeonDefinitions { get; set; }

}
