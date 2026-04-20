using System;
using System.Collections.Generic;
using Project.Gameplay.Station.Domain;
using UnityEngine;

namespace Project.Gameplay.Station.Configs
{
	[Serializable]
	public class TrainScenarioConfig
	{
		[Tooltip("Unique case ID for debugging and analytics.")]
		public string id = string.Empty;

		[Tooltip("Manifest shown to player for this case.")]
		public ManifestDataConfig manifest = new ManifestDataConfig();

		[Tooltip("Real cargo ID in wagon for judge evaluation.")]
		public string actual_cargo_id = string.Empty;

		[Tooltip("Real cargo list in wagon with quantities. Preferred over legacy actual_cargo_id.")]
		public List<CargoQuantityConfig> actual_cargo = new List<CargoQuantityConfig>();

		[Tooltip("Money earned when player verdict is correct.")]
		[Min(0)]
		public int reward_on_correct_decision = 50;

		public void ParseConfig()
		{
			id = StationConfigText.Normalize(id);
			actual_cargo_id = StationConfigText.Normalize(actual_cargo_id);
			actual_cargo ??= new List<CargoQuantityConfig>();
			for (var i = 0; i < actual_cargo.Count; i++)
			{
				actual_cargo[i] ??= new CargoQuantityConfig();
				actual_cargo[i].ParseConfig();
			}

			manifest ??= new ManifestDataConfig();
			manifest.ParseConfig();
		}

		public TrainScenario ToDomain()
		{
			ParseConfig();
			return new TrainScenario(
				id,
				manifest.ToDomain(),
				BuildActualCargoEntries(),
				reward_on_correct_decision);
		}

		private List<CargoQuantity> BuildActualCargoEntries()
		{
			var result = new List<CargoQuantity>();
			for (var i = 0; i < actual_cargo.Count; i++)
			{
				var entry = actual_cargo[i]?.ToDomain();
				if (entry == null || entry.IsValid == false)
				{
					continue;
				}

				result.Add(entry);
			}

			if (result.Count == 0 && string.IsNullOrWhiteSpace(actual_cargo_id) == false)
			{
				result.Add(new CargoQuantity(actual_cargo_id, 1));
			}

			return result;
		}
	}
}
