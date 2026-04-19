using System;
using Project.Gameplay.Station.Domain;
using UnityEngine;

namespace Project.Gameplay.Station.Configs
{
	[Serializable]
	public sealed class CargoQuantityConfig
	{
		[Tooltip("Cargo ID used by cargo catalog and runtime judge.")]
		public string cargo_id = string.Empty;

		[Tooltip("How many visible cargo units of this type are inside the wagon.")]
		[Min(0)]
		public int quantity = 1;

		public void ParseConfig()
		{
			cargo_id = StationConfigText.Normalize(cargo_id);
			if (quantity < 0)
			{
				quantity = 0;
			}
		}

		public CargoQuantity ToDomain()
		{
			ParseConfig();
			return new CargoQuantity(cargo_id, quantity);
		}
	}
}
