using Saandy;
using Sandbox;
using System.Numerics;
using System.Threading.Tasks;
using static DungeonDefinition;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Dungeon;

public static class DungeonGenerator
{
	private static readonly Vector3 DungeonOrigin = new Vector3( 0, 0, 0 );

	private static DungeonDefinition.Biome CurrentBiome { get; set; }

	private static List<RoomSetup> SpawnedRooms;

	public static void GenerateDungeon(DungeonDefinition def)
	{

		SpawnedRooms = new List<RoomSetup>();

		CurrentBiome = def.RandomBiome;

		RoomSetup entrance = new RoomSetup( CurrentBiome.RandomEntrance );
		entrance.GameObject.Transform.Position = DungeonOrigin;
		entrance.InitiateBounds();

		SpawnedRooms.Add( entrance );

		CreateRoomConnection( ref entrance, 15);

	}

	private static void CreateRoomConnection(ref RoomSetup prevRoom, int iterations)
	{

		if ( iterations <= 0 ) { return; }

		//RoomSetup currentRoom = TrySpawnRoomThatFits(ref prevRoom);
		RoomSetup currentRoom = new RoomSetup( CurrentBiome.RandomRoom );
		currentRoom.AttachTo( prevRoom );
		SpawnedRooms.Add( currentRoom );

		prevRoom?.DeleteActivePortal();
		currentRoom?.DeleteActivePortal();


		if ( currentRoom != null )
		{
			// Continue to next room in branch
			CreateRoomConnection( ref currentRoom, --iterations );
		}
		/*
		else if(prevRoom.HasUnexploredPortals)
		{
			//Start new branch
			iterations = 10;
			Log.Info( "new branch" );
			CreateRoomConnection( ref prevRoom, --iterations );
		}
		*/

	}

	private static RoomSetup TrySpawnRoomThatFits(ref RoomSetup prevRoom)
	{
		RoomSetup currentRoom = null;

		for ( int i = 0; i < 2; i++ )
		{
			currentRoom = new RoomSetup( CurrentBiome.RandomRoom );
			currentRoom.AttachTo( prevRoom );
			currentRoom.InitiateBounds();

			foreach ( RoomSetup room in SpawnedRooms )
			{
				if ( room.Data.Bounds.Overlaps( currentRoom.Data.Bounds ) )
				{
					Log.Info( "overlaps! can't spawn." );
					currentRoom.Destroy();
					currentRoom = null;
					break;
				}
			}

			// Room doesn't overlap any room!
			if ( currentRoom != null ) { break; }
		}

		SpawnedRooms.Add( currentRoom );

		return currentRoom;
	}

	//private static bool OverlapsWithSpawnedRoom( ref RoomSetup prevRoom, RoomSetup potentialRoom )
	//{
	//	GameObject go =	potentialRoom.prefabScene;
	//	GameTransform portalTx = potentialRoom.Portal.Transform;
	//	go.Transform.Rotation =  go.Transform.Rotation.Angles().Forward * portalTx.Rotation.Angles();
	//}

	private class RoomSetup
	{
		public GameObject GameObject;
		public RoomData Data;
		public int ActivePortalId = -1;
		public RoomPortal ActivePortal => Data.Portals[ActivePortalId];

		public RoomSetup(DungeonDefinition.Room room)
		{
			GameObject = SceneUtility.GetPrefabScene( ResourceLibrary.Get<PrefabFile>( room.Prefab ) ).Clone();
			GameObject.BreakFromPrefab();
			Data = GameObject.Components.Get<RoomData>();
			DeleteActivePortal();
		}

		public void AttachTo(RoomSetup prevRoom)
		{
			GameTransform prevPortal = prevRoom.ActivePortal.Transform;
			GameTransform newPortal = ActivePortal.Transform;

			GameObject.Transform.Rotation = prevPortal.Rotation.Angles() - newPortal.LocalRotation.Angles() - new Angles( 0, 180, 0 );
			//GameObject.Transform.Rotation = prevPortal.Rotation.RotateAroundAxis( prevPortal.Rotation.Up, 180 ).Angles() - newPortal.LocalRotation.Angles();
			GameObject.Transform.Position = prevPortal.Position - newPortal.Position;
		}

		public void InitiateBounds()
		{
			//move bounding box
			Transform transform = new Transform( GameObject.Transform.Position, GameObject.Transform.Rotation, GameObject.Transform.Scale );
			Data.Bounds = Data.Bounds.Transform( transform );
		}


		/// <summary>
		/// Delete old portal and get new one.
		/// </summary>
		public void DeleteActivePortal()
		{
			// Remove old portal unles old portal is null
			if ( ActivePortalId != -1 ) {
				int id = ActivePortalId;
				ActivePortal.GameObject.Destroy();
				Data.Portals.RemoveAt( id );
			}
			// Get new portal from remaining pool
			ActivePortalId = LethalGame.Random.Next( 0, Data.Portals.Count );
		}

		public void Destroy()
		{
			GameObject.Destroy();
		}

	}

}
