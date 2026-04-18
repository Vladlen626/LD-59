using System.Collections.Generic;
using PlatformCore.Services;
using Project.Gameplay.Station.Configs;
using Project.Gameplay.Station.Domain;
using Project.Gameplay.Station.Views;
using UnityEngine;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace Project.Gameplay.Scene
{
	public sealed class SceneContext : MonoBehaviour, ISceneContext
	{
		[SerializeField] [Min(0)] private int _initialDebt = 1_000;
		[SerializeField] private Transform _trainSpawnPoint;
		[SerializeField] private Transform _trainSpawnParent;
		[SerializeField] private TrainCallInteractableView _trainCallButton;
		[SerializeField] private DecisionLeverInteractableView _allowDecisionLever;
		[SerializeField] private DecisionLeverInteractableView _rejectDecisionLever;
		[SerializeField] private DispatchButtonInteractableView _dispatchButton;
		[SerializeField] private Transform _interactionRayOrigin;
		[SerializeField] [Min(0.5f)] private float _interactionDistance = 4f;
		[SerializeField] private LayerMask _interactionMask = ~0;
		[SerializeField] private StationDayConfig _dayConfig = new StationDayConfig();

		public UnityScene Scene { get; private set; }
		public TrainCallInteractableView TrainCallButton => _trainCallButton;
		public DecisionLeverInteractableView AllowDecisionLever => _allowDecisionLever;
		public DecisionLeverInteractableView RejectDecisionLever => _rejectDecisionLever;
		public DispatchButtonInteractableView DispatchButton => _dispatchButton;
		public Transform InteractionRayOrigin => _interactionRayOrigin;
		public float InteractionDistance => _interactionDistance;
		public LayerMask InteractionMask => _interactionMask;

		public DayRuleSet BuildRuleSet()
		{
			return GetOrCreateDayConfig().BuildRuleSet();
		}

		public List<TrainScenario> BuildTrainScenarios()
		{
			return GetOrCreateDayConfig().BuildTrainScenarios();
		}

		public SceneRuntimeSetup BuildRuntimeSetup()
		{
			var dayConfig = GetOrCreateDayConfig();
			var ruleSet = dayConfig.BuildRuleSet();
			var scenarios = dayConfig.BuildTrainScenarios();

			return new SceneRuntimeSetup(
				ruleSet,
				_initialDebt,
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
			DayRuleSet dayRuleSet,
			int initialDebt,
			List<TrainScenario> scenarios,
			Transform trainSpawnPoint,
			Transform trainSpawnParent)
		{
			DayRuleSet = dayRuleSet;
			InitialDebt = Mathf.Max(0, initialDebt);
			Scenarios = scenarios ?? new List<TrainScenario>();
			TrainSpawnPoint = trainSpawnPoint;
			TrainSpawnParent = trainSpawnParent;
		}

		public DayRuleSet DayRuleSet { get; }
		public int InitialDebt { get; }
		public List<TrainScenario> Scenarios { get; }
		public Transform TrainSpawnPoint { get; }
		public Transform TrainSpawnParent { get; }
	}
}
