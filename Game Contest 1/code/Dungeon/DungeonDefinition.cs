using System.Text.Json.Serialization;
using static DungeonDefinition;
using static DungeonDefinition.Biome;

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

	[JsonIgnore][Hide] public Biome RandomBiome => Biomes[GetRandomIndex( Biomes )];

	[JsonIgnore][Hide] public Door RandomDoor => Doors[GetRandomIndex( Doors )];
	[JsonIgnore][Hide] public Door RandomEntranceDoor => EntranceDoors[GetRandomIndex( EntranceDoors )];

	private static int GetRandomIndex<T>( List<T> pool ) where T : IWeighted
	{
		if(pool.Count == 1) { return 0; }

		// Get the total sum of all the weights.
		int weightSum = 0;
		for ( int i = 0; i < pool.Count(); ++i )
		{
			weightSum += pool[i].Disabled ? 0 : pool[i].Weight;
		}

		// Step through all the possibilities, one by one, checking to see if each one is selected.
		int index = 0;
		int lastIndex = pool.Count() - 1;
		while ( index < lastIndex )
		{
			if ( pool[index].Disabled ) { continue; }

			// Do a probability check with a likelihood of weights[index] / weightSum.
			if ( LethalGameManager.Random.Next( 0, weightSum ) < pool[index].Weight )
			{
				return index;
			}

			// Remove the last item from the sum of total untested weights and try again.
			weightSum -= pool[index++].Weight;
		}

		// No other item was selected, so return very last index.
		return index;
	}


	public class Biome : IWeighted
	{
		[JsonInclude] public bool Disabled { get; set; }

		[Description( "Just a formality." )]
		[JsonInclude] public string Title { get; set; }

		[Description( "How likely is this biome to appear?" )]
		[JsonInclude][Range( 0, 100 )] public int Weight { get; set; }

		[Description("How big this biome can be (room count) at most.")]
		[JsonInclude][Range( 1, 100 )] public int Continuance { get; set; }

		[Description( "All etrance rooms possible for this Biome." )]
		[JsonInclude] public List<Room> EntranceRooms { get; set; }

		[Description( "All spawnable rooms in this dungeon." )]
		[JsonInclude] public List<Room> Rooms { get; set; }

		[JsonIgnore][Hide] public Room RandomEntrance => EntranceRooms[GetRandomIndex( EntranceRooms )];
		[JsonIgnore][Hide] public Room RandomRoom => Rooms[GetRandomIndex( Rooms )];

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

}
