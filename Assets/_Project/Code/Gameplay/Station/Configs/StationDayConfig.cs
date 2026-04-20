using System;
using System.Collections.Generic;
using Project.Gameplay.Station.Domain;
using Project.Gameplay.Station.Runtime;
using UnityEngine;

namespace Project.Gameplay.Station.Configs
{
	[Serializable]
	public sealed class StationDayConfig : BaseConfig
	{
		public const int MaxTrainScenariosPerDay = 8;

		[Tooltip("Debt value at campaign/fallback start. In campaign mode only the first loaded day seeds the shared debt.")]
		[Min(0)]
		public int initial_debt = 1000;

		[Tooltip("Display/index number of the day in logs and summary. Starts from 1.")]
		[Min(1)]
		public int day_index = 1;

		[Tooltip("Fine applied when player allows train with hidden/forbidden violation.")]
		[Min(0)]
		public int missed_contraband_penalty = 100;

		[Tooltip("Fine applied when player sends back a clean train.")]
		[Min(0)]
		public int wrongful_rejection_penalty = 40;

		[Tooltip("Cargo IDs that are forbidden for this day even if declared in manifest.")]
		public List<string> forbidden_cargo_ids = new List<string>();

		[Header("Narrative")]
		[Tooltip("Shift-start letter shown before gameplay begins.")]
		public NightShiftLetterConfig shift_letter = new NightShiftLetterConfig();

		[Tooltip("Optional override used on the last authored day as a final warning.")]
		public NightShiftLetterConfig final_notice = new NightShiftLetterConfig();

		[Tooltip("Basic cost of living deducted in the end-of-shift settlement.")]
		public DayLivingCostConfig living_cost = new DayLivingCostConfig();

		[Tooltip("Optional one-line end-of-shift narrative shown on summary.")]
		public string end_day_note = string.Empty;

		[Tooltip("Ordered inspection cases for the day.")]
		public List<TrainScenarioConfig> train_scenarios = new List<TrainScenarioConfig>();

		public override void ParseConfig()
		{
			if (initial_debt < 0)
			{
				initial_debt = 0;
			}

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

			shift_letter ??= new NightShiftLetterConfig();
			shift_letter.ParseConfig();

			final_notice ??= new NightShiftLetterConfig();
			final_notice.ParseConfig();

			living_cost ??= new DayLivingCostConfig();
			living_cost.ParseConfig();
			end_day_note = StationConfigText.Normalize(end_day_note);

			if (train_scenarios == null)
			{
				train_scenarios = new List<TrainScenarioConfig>();
			}

			if (train_scenarios.Count > MaxTrainScenariosPerDay)
			{
				train_scenarios.RemoveRange(
					MaxTrainScenariosPerDay,
					train_scenarios.Count - MaxTrainScenariosPerDay);
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

		public NightShiftLetterData BuildShiftLetterData()
		{
			ParseConfig();
			return shift_letter.ToRuntimeData();
		}

		public NightShiftLetterData BuildFinalNoticeData()
		{
			ParseConfig();
			return final_notice.ToRuntimeData();
		}

		public DayLivingCostData BuildLivingCostData()
		{
			ParseConfig();
			return living_cost.ToRuntimeData();
		}

		public string BuildEndDayNote()
		{
			ParseConfig();
			return end_day_note;
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
