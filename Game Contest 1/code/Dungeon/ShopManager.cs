using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class ShopManager
{
	public static List<ShopItem> Items { get; private set; } = new()
	{
		new ShopItem("Flashlight", "prefabs/items/flashlight.prefab", 20),
		new ShopItem("Key", "prefabs/items/key.prefab", 10)
	};

	public class ShopItem
	{
		public string Name { get; set; }
		public string PrefabPath { get; set; }
		public int Cost { get; set; }
		
		public ShopItem( string name, string prefabPath, int cost ) 
		{
			this.Name = name;
			this.PrefabPath = prefabPath;
			this.Cost = cost;
		}
	}

}
