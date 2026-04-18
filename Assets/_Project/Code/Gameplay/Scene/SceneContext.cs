using System.Collections.Generic;
using PlatformCore.Services;
using Project.Gameplay.Station.Configs;
using Project.Gameplay.Station.Domain;
using Project.Gameplay.Station.Runtime;
using UnityEngine;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace Project.Gameplay.Scene
{
	public sealed class SceneContext : MonoBehaviour, ISceneContext
	{
		[SerializeField] [Min(0)] private int _initialDebt = 1_000;
		[SerializeField] private Transform _trainSpawnPoint;
		[SerializeField] private Transform _trainSpawnParent;
		[SerializeField] private StationDayConfig _dayConfig = new StationDayConfig();

		public UnityScene Scene { get; private set; }

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

		public SceneRuntimeSetup BuildRuntimeSetup()
		{
			var dayConfig = GetOrCreateDayConfig();
			var ruleSet = dayConfig.BuildRuleSet();
			var scenarios = dayConfig.BuildTrainScenarios();
			var runtimeModel = new StationRuntimeModel(
				new StationDayModel(ruleSet),
				new DebtModel(_initialDebt),
				new InspectionJournalModel());

			return new SceneRuntimeSetup(
				runtimeModel,
				scenarios,
				_trainSpawnPoint,
				_trainSpawnParent);
		}

		private void Awake()
		{
			Scene = gameObject.scene;
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

	public readonly struct SceneRuntimeSetup
	{
		public SceneRuntimeSetup(
			StationRuntimeModel runtimeModel,
			List<TrainScenario> scenarios,
			Transform trainSpawnPoint,
			Transform trainSpawnParent)
		{
			RuntimeModel = runtimeModel;
			Scenarios = scenarios ?? new List<TrainScenario>();
			TrainSpawnPoint = trainSpawnPoint;
			TrainSpawnParent = trainSpawnParent;
		}

		public StationRuntimeModel RuntimeModel { get; }
		public List<TrainScenario> Scenarios { get; }
		public Transform TrainSpawnPoint { get; }
		public Transform TrainSpawnParent { get; }
	}
}
