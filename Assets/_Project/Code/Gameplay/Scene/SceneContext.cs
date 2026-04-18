using PlatformCore.Services;
using Project.Gameplay.Station.Configs;
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
		[SerializeField] private StationActionButtonInteractableView _actionButton;
		[SerializeField] private StationDecisionToggleInteractableView _decisionToggle;
		[SerializeField] private Transform _interactionRayOrigin;
		[SerializeField] [Min(0.5f)] private float _interactionDistance = 4f;
		[SerializeField] private LayerMask _interactionMask = ~0;
		[SerializeField] private StationDayConfig _dayConfig = new StationDayConfig();

		public UnityScene Scene { get; private set; }
		public StationActionButtonInteractableView ActionButton => _actionButton;
		public StationDecisionToggleInteractableView DecisionToggle => _decisionToggle;
		public Transform InteractionRayOrigin => _interactionRayOrigin;
		public float InteractionDistance => _interactionDistance;
		public LayerMask InteractionMask => _interactionMask;

		public SceneRuntimeSetup BuildRuntimeSetup()
		{
			var dayConfig = GetOrCreateDayConfig();
			dayConfig.BuildRuntimeData(out var ruleSet, out var scenarios);

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
}
