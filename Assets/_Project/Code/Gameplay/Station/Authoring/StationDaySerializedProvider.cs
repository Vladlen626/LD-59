using System.Collections.Generic;
using Project.Gameplay.Station.Configs;
using Project.Gameplay.Station.Domain;
using Project.Gameplay.Station.Runtime;
using UnityEngine;

namespace Project.Gameplay.Station.Authoring
{
	public sealed class StationDaySerializedProvider : MonoBehaviour
	{
		[SerializeField] [Min(0)] private int _initialDebt = 1_000;
		[SerializeField] private StationDayConfig _dayConfig = new StationDayConfig();

		public DayRuleSet BuildRuleSet()
		{
			return GetOrCreateDayConfig().BuildRuleSet();
		}

		public List<TrainScenario> BuildTrainScenarios()
		{
			return GetOrCreateDayConfig().BuildTrainScenarios();
		}

		public StationRuntimeModel BuildRuntimeModel()
		{
			var stationDayModel = new StationDayModel(BuildRuleSet());
			var debtModel = new DebtModel(_initialDebt);
			var inspectionJournal = new InspectionJournalModel();

			return new StationRuntimeModel(
				stationDayModel,
				debtModel,
				inspectionJournal);
		}

		public StationDayRuntimeSetup BuildRuntimeSetup()
		{
			var dayConfig = GetOrCreateDayConfig();
			var ruleSet = dayConfig.BuildRuleSet();
			var scenarios = dayConfig.BuildTrainScenarios();
			var runtimeModel = new StationRuntimeModel(
				new StationDayModel(ruleSet),
				new DebtModel(_initialDebt),
				new InspectionJournalModel());

			return new StationDayRuntimeSetup(runtimeModel, scenarios);
		}

		private void OnValidate()
		{
			if (_dayConfig == null)
			{
				_dayConfig = new StationDayConfig();
			}

			_dayConfig.ParseConfig();
		}

		private StationDayConfig GetOrCreateDayConfig()
		{
			if (_dayConfig == null)
			{
				_dayConfig = new StationDayConfig();
			}

			return _dayConfig;
		}
	}

	public readonly struct StationDayRuntimeSetup
	{
		public StationDayRuntimeSetup(
			StationRuntimeModel runtimeModel,
			List<TrainScenario> scenarios)
		{
			RuntimeModel = runtimeModel;
			Scenarios = scenarios ?? new List<TrainScenario>();
		}

		public StationRuntimeModel RuntimeModel { get; }
		public List<TrainScenario> Scenarios { get; }
	}
}
