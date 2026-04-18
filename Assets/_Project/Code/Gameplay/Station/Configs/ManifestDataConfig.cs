using System;
using Project.Gameplay.Station.Domain;

namespace Project.Gameplay.Station.Configs
{
	[Serializable]
	public class ManifestDataConfig
	{
		public string declared_cargo_id = string.Empty;
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
