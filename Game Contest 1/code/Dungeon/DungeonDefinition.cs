using System.Text.Json.Serialization;

[GameResource( "Dungeon Definition", "dungeon", "This asset defines a procedural dungeon with unique rooms.")]
public partial class DungeonDefinition : GameResource
{
	[JsonInclude] public string Title { get; set; }

	[JsonInclude] public List<Biome> Biomes { get; set; }

	[JsonIgnore][Hide] public Biome RandomBiome => Biomes[LethalGame.Random.Next( 0, Biomes.Count() )];

	public class Biome
	{
		[JsonInclude] public string Title { get; set; }
		[JsonInclude] public List<Room> Entrances { get; set; }
		[JsonInclude] public List<Room> Rooms { get; set; }

		[JsonIgnore][Hide] public Room RandomEntrance => Entrances[GetRandomRoomIndex( Entrances )];
		[JsonIgnore][Hide] public Room RandomRoom => Rooms[GetRandomRoomIndex( Rooms )];

		private int GetRandomRoomIndex( List<Room> pool )
		{
			// Get the total sum of all the weights.
			int weightSum = 0;
			for ( int i = 0; i < pool.Count(); ++i )
			{
				weightSum += pool[i].Weight;
			}

			// Step through all the possibilities, one by one, checking to see if each one is selected.
			int index = 0;
			int lastIndex = pool.Count() - 1;
			while ( index < lastIndex )
			{
				// Do a probability check with a likelihood of weights[index] / weightSum.
				if ( LethalGame.Random.Next( 0, weightSum ) < pool[index].Weight )
				{
					return index;
				}

				// Remove the last item from the sum of total untested weights and try again.
				weightSum -= pool[index++].Weight;
			}

			// No other item was selected, so return very last index.
			return index;
		}

	}

	public class Room
	{
		[JsonInclude][Range(0, 100)] public int Weight { get; set; }
		[JsonInclude][ResourceType( "prefab" )] public string Prefab { get; set; }
	}

}
