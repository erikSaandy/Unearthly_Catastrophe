using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Dungeon;

[GameResource( "Dungeon Definition", "dungeon", "This asset defines a procedural dungeon with unique rooms.")]
public partial class DungeonDefinition : GameResource
{
	public string Title { get; set; }

	public DungeonBiome[] Biomes { get; set; }

	public struct DungeonBiome
	{
		public string Title { get; set; }

		[ResourceType( "prefab" )]
		public Room[] Entrances { get; set; }

		public Room[] Rooms { get; set; }

		[JsonIgnore][Hide] public Room GetRandomEntrance => Entrances[GetRandomRoomIndex( Entrances )];
		[JsonIgnore][Hide] public Room GetRandomRoom => Entrances[GetRandomRoomIndex( Rooms )];

		private int GetRandomRoomIndex( Room[] rooms )
		{
			// Get the total sum of all the weights.
			int weightSum = 0;
			for ( int i = 0; i < Rooms.Count(); ++i )
			{
				weightSum += Rooms[i].Weight;
			}

			// Step through all the possibilities, one by one, checking to see if each one is selected.
			int index = 0;
			int lastIndex = Rooms.Count() - 1;
			while ( index < lastIndex )
			{
				// Do a probability check with a likelihood of weights[index] / weightSum.
				if ( Game.Random.Next( 0, weightSum ) < Rooms[index].Weight )
				{
					return index;
				}

				// Remove the last item from the sum of total untested weights and try again.
				weightSum -= Rooms[index++].Weight;
			}

			// No other item was selected, so return very last index.
			return index;
		}

	}

	public struct Room
	{
		[Range(0, 100)] public int Weight { get; set; }
		[ResourceType( "prefab" )] public string Prefab { get; set; }
	}

}
