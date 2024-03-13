using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ScrapManager;

public static class MonsterManager
{

	public static List<MonsterData> MonsterInstances { get; private set; } = new()
	{
		new MonsterData("Zombie", "prefabs/monsters/zombie.prefab", strength: 2, spawnCountLimit: 4 ),
	};

	private static List<MonsterData> SpawnedMonsterData { get; set; } = new();

	public static int GetSpawnedMonsterCount( MonsterData monsterType ) => SpawnedMonsterData.FindAll( x => (x.Name == monsterType.Name) ).Count;

	public static void SpawnMonsters(List<Guid> potentialSpawners)
	{

		SpawnedMonsterData = new();

		List<string> prefabs = GetMonsterList();

		for (int i = 0; i < prefabs.Count; i++ )
		{
			Guid spawnerId = potentialSpawners[LethalGameManager.Random.Next( 0, potentialSpawners.Count )];
			GameObject spawner = LethalGameManager.Instance.Scene.Directory.FindByGuid( spawnerId );


			PrefabFile pf = null;
			if ( !ResourceLibrary.TryGet<PrefabFile>( prefabs[i], out pf ) ) { Log.Warning( $"monster {prefabs[i]} could not be retrieved!" ); continue; }
			GameObject monsterObj = SceneUtility.GetPrefabScene( pf ).Clone();
			//monsterObj.BreakFromPrefab();

			monsterObj.Transform.Position = spawner.Transform.Position;
			monsterObj.Tags.Add( "monster" );
			monsterObj.NetworkSpawn();
		}


		Log.Info( $"Spawned {prefabs.Count} monsters." );

	}

	public static void DeleteAllMonsters()
	{
		List<Monster> monsters = LethalGameManager.Instance.Scene.GetAllComponents<Monster>().ToList();
		foreach(Monster monster in monsters) { monster.GameObject.Destroy(); }
	}

	private static List<string> GetMonsterList()
	{
		List<string> result = new List<string>();

		int maxStregth = 16;
		int currentStrength = 0;

		int maxIterations = 20;
		while(currentStrength < maxStregth && maxIterations > 0) 
		{
			MonsterData monster = GetRandomMonster( maxStregth, ref currentStrength );

			// Couldn't find a monster.
			if(monster == null) { break; }

			result.Add( monster.FilePath );

			maxIterations--;
		}

		return result;

	}

	private static MonsterData GetRandomMonster(int maxStregth, ref int currentStrength)
	{
		if(currentStrength >= maxStregth) { return null; }

		int newMonsterMaxStrength = maxStregth - currentStrength;

		for (int i = 0; i < 10; i++ )
		{
			MonsterData monster = RandomMonster;

			// strength not exceeded
			if(monster.Strength <= newMonsterMaxStrength )
			{
				int spawnedOfType = GetSpawnedMonsterCount( monster );

				// Hasn't exceeded spawn limit!
				if (GetSpawnedMonsterCount(monster) < monster.SpawnCountLimit)
				{
					currentStrength += monster.Strength;
					SpawnedMonsterData.Add( monster );
					return monster;
				}
			}


		}

		// Couldn't find monster.
		return null;

	}

	public static MonsterData RandomMonster => LethalGameManager.Random.FromList( MonsterInstances );

	public class MonsterData
	{
		public string Name { get; set; }
		public string FilePath { get; set; }
		public int Strength { get; set; }
		public int SpawnCountLimit { get; set; }

		public MonsterData( string name, string filePath, int strength, int spawnCountLimit )
		{
			Name = name;
			FilePath = filePath;
			Strength = strength;
			SpawnCountLimit = spawnCountLimit;
		}

	}
	
}
