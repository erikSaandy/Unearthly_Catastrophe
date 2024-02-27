using System.Text.Json.Serialization;

[GameResource( "Moon Definition", "moon", "This asset defines a destination for players to travel to." )]
public partial class MoonDefinition : GameResource
{
	[JsonInclude][Property][Range(0, 2000)] public int TravelCost { get; set; }
	
	//TODO: Dungeon size stuff, loot potnetial, danger

	[ResourceType( "vmap" )][JsonInclude][Property] public string MapPath { get; set; }
}		
