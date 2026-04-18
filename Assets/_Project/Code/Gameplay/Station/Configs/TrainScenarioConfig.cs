using System;
using Project.Gameplay.Station.Domain;

namespace Project.Gameplay.Station.Configs
{
	[Serializable]
	public class TrainScenarioConfig
	{
		public string id = string.Empty;
		public ManifestDataConfig manifest = new ManifestDataConfig();
		public string actual_cargo_id = string.Empty;
		public bool has_hidden_contraband;
		public bool has_illegal_marker;
		public int reward_on_correct_decision = 50;

		public void ParseConfig()
		{
			id = StationConfigText.Normalize(id);
			actual_cargo_id = StationConfigText.Normalize(actual_cargo_id);
			manifest ??= new ManifestDataConfig();
			manifest.ParseConfig();
		}

		public TrainScenario ToDomain()
		{
			ParseConfig();
			return new TrainScenario(
				id,
				manifest.ToDomain(),
				actual_cargo_id,
				has_hidden_contraband,
				has_illegal_marker,
				reward_on_correct_decision);
		}
	}
}
