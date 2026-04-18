using System;
using Project.Gameplay.Station.Domain;
using UnityEngine;

namespace Project.Gameplay.Station.Configs
{
	[Serializable]
	public class ManifestDataConfig
	{
		[Tooltip("Cargo ID declared in documents and shown to player.")]
		public string declared_cargo_id = string.Empty;

		[Tooltip("Human-readable manifest description shown to player.")]
		[TextArea(1, 3)]
		public string declared_description = string.Empty;

		public void ParseConfig()
		{
			declared_cargo_id = StationConfigText.Normalize(declared_cargo_id);
			declared_description = StationConfigText.Normalize(declared_description);
		}

		public ManifestData ToDomain()
		{
			ParseConfig();
			return new ManifestData(declared_cargo_id, declared_description);
		}
	}
}
