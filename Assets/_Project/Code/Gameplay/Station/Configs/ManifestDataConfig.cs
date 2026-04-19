using System;
using System.Collections.Generic;
using Project.Gameplay.Station.Domain;
using UnityEngine;

namespace Project.Gameplay.Station.Configs
{
	[Serializable]
	public class ManifestDataConfig
	{
		[Tooltip("Cargo ID declared in documents and shown to player.")]
		public string declared_cargo_id = string.Empty;

		[Tooltip("Declared cargo list with quantities. Preferred over legacy declared_cargo_id.")]
		public List<CargoQuantityConfig> declared_cargo = new List<CargoQuantityConfig>();

		[Tooltip("Human-readable manifest description shown to player.")]
		[TextArea(1, 3)]
		public string declared_description = string.Empty;

		public void ParseConfig()
		{
			declared_cargo_id = StationConfigText.Normalize(declared_cargo_id);
			declared_cargo ??= new List<CargoQuantityConfig>();
			for (var i = 0; i < declared_cargo.Count; i++)
			{
				declared_cargo[i] ??= new CargoQuantityConfig();
				declared_cargo[i].ParseConfig();
			}

			declared_description = StationConfigText.Normalize(declared_description);
		}

		public ManifestData ToDomain()
		{
			ParseConfig();
			return new ManifestData(BuildDeclaredCargoEntries(), declared_description);
		}

		private List<CargoQuantity> BuildDeclaredCargoEntries()
		{
			var result = new List<CargoQuantity>();
			for (var i = 0; i < declared_cargo.Count; i++)
			{
				var entry = declared_cargo[i]?.ToDomain();
				if (entry == null || entry.IsValid == false)
				{
					continue;
				}

				result.Add(entry);
			}

			if (result.Count == 0 && string.IsNullOrWhiteSpace(declared_cargo_id) == false)
			{
				result.Add(new CargoQuantity(declared_cargo_id, 1));
			}

			return result;
		}
	}
}
