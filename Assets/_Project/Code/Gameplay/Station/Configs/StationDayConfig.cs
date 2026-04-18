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

		public void BuildRuntimeData(out DayRuleSet ruleSet, out List<TrainScenario> scenarios)
		{
			ParseConfig();
			ruleSet = BuildRuleSetFromParsed();
			scenarios = BuildTrainScenariosFromParsed();
		}

		private DayRuleSet BuildRuleSetFromParsed()
		{
			return new DayRuleSet(
				day_index,
				forbidden_cargo_ids,
				missed_contraband_penalty,
				wrongful_rejection_penalty);
		}

		private List<TrainScenario> BuildTrainScenariosFromParsed()
		{
			var result = new List<TrainScenario>(train_scenarios.Count);
			for (var i = 0; i < train_scenarios.Count; i++)
			{
				result.Add(train_scenarios[i].ToDomain());
			}

			return result;
		}
	}
}
