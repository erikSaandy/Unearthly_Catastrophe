using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class ScrapManager
{
	public static string RandomScrap => ScrapPrefabs.GetRandom().FilePath;

	public static List<ScrapPrefab> ScrapPrefabs { get; private set; } = new()
	{
		new ScrapPrefab("prefabs/scraps/scrap_cube.prefab" , 50),
		new ScrapPrefab("prefabs/scraps/scrap_die.prefab" , 50),
		new ScrapPrefab("prefabs/scraps/scrap_gift_box.prefab" , 50),
		new ScrapPrefab("prefabs/scraps/scrap_gold_bar.prefab" , 50),
		new ScrapPrefab("prefabs/scraps/scrap_horn.prefab" , 50),
		new ScrapPrefab("prefabs/scraps/scrap_magnifying_glass.prefab" , 50),
		new ScrapPrefab("prefabs/scraps/scrap_metal_sheet.prefab" , 50),
		new ScrapPrefab("prefabs/scraps/scrap_old_phone.prefab" , 50),
		new ScrapPrefab("prefabs/scraps/scrap_phone_book.prefab" , 50),
		new ScrapPrefab("prefabs/scraps/scrap_soda_can.prefab" , 50),

	};

	public class ScrapPrefab : IWeighted
	{
		public string FilePath { get; private set; }

		public bool Disabled { get; set; }
		public int Weight { get; set; }

		public ScrapPrefab(string filePath, int weight) 
		{
			this.FilePath = filePath;
			this.Weight = weight;
		}
	}

}
