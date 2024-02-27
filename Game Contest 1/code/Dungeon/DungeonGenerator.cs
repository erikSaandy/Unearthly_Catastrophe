using static DungeonDefinition;

namespace Dungeon;

public static class DungeonGenerator
{
	private static readonly Vector3 DungeonOrigin = new Vector3( 0, 0, 0 );

	private static DungeonDefinition DungeonResource { get; set; }

	private static List<RoomSetup> SpawnedRooms;

	private const int MainIterations = 15;

	public static void GenerateDungeon(DungeonDefinition def)
	{

		SpawnedRooms = new List<RoomSetup>();

		DungeonResource = def;
		Biome biome = DungeonResource.RandomBiome;

		RoomSetup entrance = new RoomSetup( biome.RandomEntrance );
		if(entrance.Data == null) { Log.Error( "Could not load prefab " + entrance.Prefab ); }

		entrance.GameObject.Transform.Position = DungeonOrigin;
		entrance.InitiateBounds();
		entrance.SpawnEntranceDoor();
		entrance.GameObject.Name = "1 (entrance)";

		SpawnedRooms.Add( entrance );

		//CreateRoomConnection( ref entrance, InitialDepth );
		SearchRooms( ref entrance, ref biome, MainIterations, 0 );

		Log.Info( $"[Generated dungeon with {SpawnedRooms.Count} rooms." );

	}

	private static void SearchRooms( ref RoomSetup currentRoom, ref Biome currentBiome, int iteration, int biomeDepth )
	{

		if ( biomeDepth >= currentBiome.Continuance )
		{
			Log.Info( "> new biome" );
			currentBiome = DungeonResource.RandomBiome;
			biomeDepth = 0;
		}

		while ( currentRoom.HasUnexploredPortals )
		{

			if ( currentRoom == SpawnedRooms[0] ) { 
				Log.Info( "[entrance has " + currentRoom.Data.Portals.Count + " portals left.]" );
				Log.Info( iteration );
			}
			
			RoomSetup nextRoom = null;

			// spawn new room if possible 
			if ( iteration > 0 ) { nextRoom = TrySpawnRoomThatFits( ref currentRoom, ref currentBiome, iteration ); }

			// was able to spawn room!
			if (nextRoom != null)
			{
				nextRoom.GameObject.Name = nextRoom.GameObject.Name + " (" + SpawnedRooms.Count.ToString() + ")";
				nextRoom.MoveToNextPortal();

				SearchRooms( ref nextRoom, ref currentBiome, --iteration, ++biomeDepth );
				//iteration = BranchIterations;

			}
			else if ( currentRoom.HasUnexploredPortals )
			{
				currentRoom.SpawnBranchCap();
			}

			currentRoom.MoveToNextPortal();

		}

	}

	private static RoomSetup TrySpawnRoomThatFits(ref RoomSetup currentRoom, ref Biome currentBiome, int iteration)
	{
		RoomSetup nextRoom = null;

		if(!currentRoom.HasUnexploredPortals) { return null; }

		for ( int i = 0; i < 4; i++ )
		{
			nextRoom = new RoomSetup( currentBiome.RandomRoom );
			if ( nextRoom.Data == null ) { Log.Error( "Could not load prefab " + nextRoom.Prefab ); }

			// If next room doesn't have a matching portal type, this room can't fit.
			if (!nextRoom.GetMatchingPortal( currentRoom.ActivePortal.PortalType )) {
				DeleteNextRoom();
				continue; 
			}

			nextRoom.AttachTo( currentRoom );
			nextRoom.InitiateBounds();

			// Does this proom overlap any already spawned rooms?
			foreach ( RoomSetup room in SpawnedRooms )
			{
				if ( room.Data.Bounds.Overlaps( nextRoom.Data.Bounds ) )
				{
					Log.Info( "overlaps! can't spawn." );
					DeleteNextRoom();
					break;
				}
			}

			// Room doesn't overlap any room!
			if ( nextRoom != null ) 
			{
				//Log.Info( i + " tries" );
				break;
			}
		}

		if( nextRoom != null ) {
			SpawnedRooms.Add( nextRoom );
			Log.Info( $"> Spawned room #{ SpawnedRooms.Count } (Iteration: {iteration})" );
		}
	
		return nextRoom;

		void DeleteNextRoom()
		{
			nextRoom?.Destroy();
			nextRoom = null;
		}
	}

	//private static bool OverlapsWithSpawnedRoom( ref RoomSetup prevRoom, RoomSetup potentialRoom )
	//{
	//	GameObject go =	potentialRoom.prefabScene;
	//	GameTransform portalTx = potentialRoom.Portal.Transform;
	//	go.Transform.Rotation =  go.Transform.Rotation.Angles().Forward * portalTx.Rotation.Angles();
	//}

	private class RoomSetup
	{
		public string Prefab { get; private set; }
		public GameObject GameObject;
		public RoomData Data = null;
		public int ActivePortalId = -1;
		public RoomPortal ActivePortal => Data.Portals[ActivePortalId];
		public bool HasUnexploredPortals => Data?.Portals == null ? false : Data?.Portals?.Count > 0;

		public RoomSetup(Room room)
		{
			Prefab = room.Prefab;
			PrefabFile pf = null;
			if(!ResourceLibrary.TryGet<PrefabFile>( room.Prefab, out pf )) { return; }
			GameObject = SceneUtility.GetPrefabScene( pf ).Clone();
			GameObject.BreakFromPrefab();
			GameObject.Name = room.Prefab;

			Data = GameObject.Components.Get<RoomData>();

			// Get new portal (doesn't delete in this case.)
			MoveToNextPortal();
		}

		public void AttachTo(RoomSetup prevRoom)
		{
			GameTransform prevPortal = prevRoom.ActivePortal.Transform;
			GameTransform newPortal = ActivePortal.Transform;

			GameObject.Transform.Rotation = prevPortal.Rotation.Angles() - newPortal.LocalRotation.Angles() - new Angles( 0, 180, 0 );
			//GameObject.Transform.Rotation = prevPortal.Rotation.RotateAroundAxis( prevPortal.Rotation.Up, 180 ).Angles() - newPortal.LocalRotation.Angles();
			GameObject.Transform.Position = prevPortal.Position - newPortal.Position;

			SpawnDoor( ActivePortal );

		}

		public void InitiateBounds()
		{
			//move bounding box
			Transform transform = new Transform( GameObject.Transform.Position, GameObject.Transform.Rotation, GameObject.Transform.Scale );
			Data.Bounds = Data.Bounds.Transform( transform ).Grow(-1);
		}

		public bool GetMatchingPortal(RoomPortal.RoomPortalType otherType)
		{
			if(ActivePortal.PortalType == otherType) { return true; }

			int count = Data.Portals.Count - 1;

			while(count > 0)
			{
				ActivePortalId = (ActivePortalId + 1) % Data.Portals.Count;
				if(ActivePortal.PortalType == otherType) { return true; }

				count--;
			}

			return false;

		}


		/// <summary>
		/// Delete old portal and get new one.
		/// </summary>
		public void MoveToNextPortal(bool deletePrevious = true)
		{
			// Remove old portal unles old portal is null
			if ( ActivePortalId != -1  && deletePrevious ) {
				int id = ActivePortalId;
				ActivePortal.GameObject.Destroy();
				Data.Portals.RemoveAt( id );
			}
			// Get new portal from remaining pool
			ActivePortalId = LethalGame.Random.Next( 0, Data.Portals.Count );
		}

		public void SpawnEntranceDoor()
		{
			for(int i = 0; i < Data.Portals.Count; i++ )
			{
				if ( Data.Portals[i].PortalType == RoomPortal.RoomPortalType.Entrance )
				{
					SpawnDoor( Data.Portals[i] );
					ActivePortalId = i;
					MoveToNextPortal();
					break;
				}
			}
		}

		public void SpawnDoor(RoomPortal portal)
		{
			GameObject door = null;

			if( portal.PortalType == RoomPortal.RoomPortalType.Corridor )
			{
				// No door.
				return;
			}
			else if ( portal.PortalType == RoomPortal.RoomPortalType.Door )
			{
				door = SceneUtility.GetPrefabScene( ResourceLibrary.Get<PrefabFile>( DungeonResource.RandomDoor.Prefab ) ).Clone();
			}
			else if ( portal.PortalType == RoomPortal.RoomPortalType.Entrance )
			{
				door = SceneUtility.GetPrefabScene( ResourceLibrary.Get<PrefabFile>( DungeonResource.RandomEntranceDoor.Prefab ) ).Clone();
			}

			door.BreakFromPrefab();

			door.Transform.Position = portal.Transform.Position;
			door.Transform.Rotation = portal.Transform.Rotation;
			door.SetParent( GameObject );
		}

		public void SpawnBranchCap()
		{
			GameObject cap = null;

			if ( ActivePortal.PortalType == RoomPortal.RoomPortalType.Corridor )
			{
				cap = SceneUtility.GetPrefabScene( ResourceLibrary.Get<PrefabFile>( DungeonResource.CorridorCap ) ).Clone();
			}
			else if ( ActivePortal.PortalType == RoomPortal.RoomPortalType.Door )
			{
				cap = SceneUtility.GetPrefabScene( ResourceLibrary.Get<PrefabFile>( DungeonResource.DoorCap ) ).Clone();
			}


			cap.BreakFromPrefab();

			cap.Transform.Position = ActivePortal.Transform.Position;
			cap.Transform.Rotation = ActivePortal.Transform.Rotation;
			cap.SetParent( GameObject );
		}

		public void Destroy()
		{
			GameObject.Destroy();
		}


	}

}