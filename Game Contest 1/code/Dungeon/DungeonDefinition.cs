using System;
using System.Text.Json.Serialization;

[GameResource( "Dungeon Definition", "dungeon", "This asset defines a procedural dungeon with unique rooms." )]
public partial class DungeonDefinition : GameResource
{
	[Description( "Just a formality." )]
	[JsonInclude] public string Title { get; set; }

	[Description( "The different looks of this dungeon." )]
	[JsonInclude] public List<Biome> Biomes { get; set; }

	[Description( "All possible entrance doors for this dungeon." )]
	[JsonInclude] public List<Door> EntranceDoors { get; set; }

	[Description( "All possible doors for this dungeon." )]
	[JsonInclude] public List<Door> Doors { get; set; }

	[Description( "Corridor cap for this dungeon " )]
	[JsonInclude][ResourceType( "prefab" )] public string CorridorCap { get; set; }

	[Description( "Door cap for this dungeon " )]
	[JsonInclude][ResourceType( "prefab" )] public string DoorCap { get; set; }

	[JsonIgnore][Hide] public Biome RandomBiome => GetRandom( Biomes );

	[JsonIgnore][Hide] public Door RandomDoor => GetRandom( Doors );
	[JsonIgnore][Hide] public Door RandomEntranceDoor => GetRandom( EntranceDoors );

	public static T GetRandom<T>( List<T> pool ) where T : IWeighted
	{
		if ( pool == null || pool.Count == 0 ) { Log.Error( $"weighted pool can not be empty." ); }

		// Only one item in list? select it.
		if ( pool.Count == 1 ) { return pool[0]; }

		int totalWeight = pool.Sum( x => x.Weight );

		int rnd = LethalGameManager.Random.Next( totalWeight );

		return pool.First( x => (rnd -= x.Weight) < 0 );

	}

}


public class Biome : IWeighted
{
	[JsonInclude] public bool Disabled { get; set; }

	[Description( "Just a formality." )]
	[JsonInclude] public string Title { get; set; }

	[Description( "How likely is this biome to appear?" )]
	[JsonInclude][Range( 0, 100 )] public int Weight { get; set; }

	[Description( "How big this biome can be (room count) at most." )]
	[JsonInclude][Range( 1, 100 )] public int Continuance { get; set; }

	[Description( "All etrance rooms possible for this Biome." )]
	[JsonInclude] public List<Room> EntranceRooms { get; set; }

	[Description( "All spawnable rooms in this dungeon." )]
	[JsonInclude] public List<Room> Rooms { get; set; }

	[JsonIgnore][Hide] public Room RandomEntrance => DungeonDefinition.GetRandom( EntranceRooms );
	[JsonIgnore][Hide] public Room RandomRoom => DungeonDefinition.GetRandom( Rooms );

}

public class Room : IWeighted
{
	[JsonInclude] public bool Disabled { get; set; }

	[Description( "How likely is this room to appear?" )]
	[JsonInclude][Range( 0, 100 )] public int Weight { get; set; }
	[JsonInclude][ResourceType( "prefab" )] public string Prefab { get; set; }

}

public class Door : IWeighted
{
	[JsonInclude] public bool Disabled { get; set; }

	[Description( "How likely is this door to appear?" )]
	[JsonInclude][Range( 0, 100 )] public int Weight { get; set; }
	[JsonInclude][ResourceType( "prefab" )] public string Prefab { get; set; }

}

public interface IWeighted {
	[JsonInclude] public bool Disabled { get; set; }
	[JsonInclude][Range( 0, 100 )] public int Weight { get; set; }

}
