using PlatformCore.Services;
using Project.Gameplay.Station.Views;
using UnityEngine;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace Project.Gameplay.Scene
{
	public sealed class SceneContext : MonoBehaviour, ISceneContext
	{
		[SerializeField] [Tooltip("World point where train prefab is spawned.")]
		private Transform _trainSpawnPoint;
		[SerializeField] [Tooltip("Optional parent for spawned train instance.")]
		private Transform _trainSpawnParent;
		[SerializeField] [Tooltip("How far behind the station the train starts before arrival.")]
		private float _trainArrivalDistance = 30f;
		[SerializeField] [Tooltip("How far forward the train leaves the station before cleanup.")]
		private float _trainDepartureDistance = 30f;
		[SerializeField] [Tooltip("Arrival duration in seconds. Uses smooth braking near the station.")]
		private float _trainArrivalDuration = 2.4f;
		[SerializeField] [Tooltip("Departure duration in seconds. Uses smooth acceleration from the station.")]
		private float _trainDepartureDuration = 1.8f;
		[SerializeField] [Tooltip("Single world button: call train if none, send case if train is present.")]
		private StationActionButtonInteractableView _actionButton;
		[SerializeField] [Tooltip("Single world toggle: switches player verdict Allow/SendBack.")]
		private StationDecisionToggleInteractableView _decisionToggle;
		[SerializeField] [Tooltip("World-space inspection info view for forbidden cargo, manifest and dispatch state.")]
		private StationInspectionInfoView _inspectionInfoView;

		public UnityScene Scene { get; private set; }
		public StationActionButtonInteractableView ActionButton => _actionButton;
		public StationDecisionToggleInteractableView DecisionToggle => _decisionToggle;
		public StationInspectionInfoView InspectionInfoView => _inspectionInfoView;

		public SceneRuntimeSetup BuildRuntimeSetup()
		{
			return new SceneRuntimeSetup(
				_trainSpawnPoint,
				_trainSpawnParent,
				_trainArrivalDistance,
				_trainDepartureDistance,
				_trainArrivalDuration,
				_trainDepartureDuration);
		}

		private void Awake()
		{
			Scene = gameObject.scene;
		}
	}
}
