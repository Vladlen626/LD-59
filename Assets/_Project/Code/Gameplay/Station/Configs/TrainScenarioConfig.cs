using System;
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

		[Tooltip("True if contraband is hidden in cargo.")]
		public bool has_hidden_contraband;

		[Tooltip("True if wagon has illegal marker that must trigger SendBack.")]
		public bool has_illegal_marker;

		[Tooltip("Money earned when player verdict is correct.")]
		[Min(0)]
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
