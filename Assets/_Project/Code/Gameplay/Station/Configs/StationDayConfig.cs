using System;
using System.Collections.Generic;
using Project.Gameplay.Station.Domain;

namespace Project.Gameplay.Station.Configs
{
	[Serializable]
	public class StationDayConfig
	{
		public int day_index = 1;
		public int missed_contraband_penalty = 100;
		public int wrongful_rejection_penalty = 40;
		public List<string> forbidden_cargo_ids = new List<string>();
		public List<TrainScenarioConfig> train_scenarios = new List<TrainScenarioConfig>();

		public void ParseConfig()
		{
			if (day_index < 1)
			{
				day_index = 1;
			}

			if (missed_contraband_penalty < 0)
			{
				missed_contraband_penalty = 0;
			}

			if (wrongful_rejection_penalty < 0)
			{
				wrongful_rejection_penalty = 0;
			}

			if (forbidden_cargo_ids == null)
			{
				forbidden_cargo_ids = new List<string>();
			}

			for (var i = 0; i < forbidden_cargo_ids.Count; i++)
			{
				var cargoId = forbidden_cargo_ids[i];
				forbidden_cargo_ids[i] = string.IsNullOrWhiteSpace(cargoId) ? string.Empty : cargoId.Trim();
			}

			if (train_scenarios == null)
			{
				train_scenarios = new List<TrainScenarioConfig>();
			}

			for (var i = 0; i < train_scenarios.Count; i++)
			{
				train_scenarios[i] ??= new TrainScenarioConfig();
				train_scenarios[i].ParseConfig();
			}
		}

		public DayRuleSet BuildRuleSet()
		{
			ParseConfig();
			return new DayRuleSet(
				day_index,
				forbidden_cargo_ids,
				missed_contraband_penalty,
				wrongful_rejection_penalty);
		}

		public List<TrainScenario> BuildTrainScenarios()
		{
			ParseConfig();

			var result = new List<TrainScenario>(train_scenarios.Count);
			for (var i = 0; i < train_scenarios.Count; i++)
			{
				result.Add(train_scenarios[i].ToDomain());
			}

			return result;
		}
	}

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

	internal static class StationConfigText
	{
		public static string Normalize(string value)
		{
			return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
		}
	}
}
