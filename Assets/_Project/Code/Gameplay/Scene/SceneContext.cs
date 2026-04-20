using System.Collections.Generic;
using PlatformCore.Services;
using Project.Gameplay.Station.Views;
using UnityEngine;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace Project.Gameplay.Scene
{
	public sealed class SceneContext : MonoBehaviour, ISceneContext
	{
		[SerializeField] [Tooltip("World point where train prefab is spawned.")]
		private Transform _spawnPoint;
		[SerializeField] [Tooltip("Optional parent for spawned train instance.")]
		private Transform _spawnParent;
		[SerializeField] [Tooltip("How far behind the station the train starts before arrival.")]
		private float _arrivalDistance = 30f;
		[SerializeField] [Tooltip("How far forward the train leaves the station before cleanup.")]
		private float _departureDistance = 30f;
		[SerializeField] [Tooltip("Arrival duration in seconds. Uses smooth braking near the station.")]
		private float _arrivalDurationSeconds = 1.846f;
		[SerializeField] [Tooltip("Departure duration in seconds. Uses smooth acceleration from the station.")]
		private float _departureDurationSeconds = 1.385f;
		[SerializeField] [Tooltip("Single world button: call train if none, send case if train is present.")]
		private StationActionButtonInteractableView _stationActionButton;
		[SerializeField] [Tooltip("Single world toggle: switches player verdict Allow/SendBack.")]
		private StationDecisionToggleInteractableView _stationDecisionToggle;
		[SerializeField] [Tooltip("World-space inspection info view for forbidden cargo, manifest and dispatch state.")]
		private StationInspectionInfoView _stationInspectionInfo;
		[SerializeField] [Tooltip("Building door that should be reset between nights.")]
		private BuildingDoorInteractableView _buildingDoor;
		[SerializeField] [Tooltip("Where the player wakes up at the start of a shift.")]
		private Transform _wakePoint;
		[SerializeField] [Tooltip("Door transform used as the source of the wake-up knock.")]
		private Transform _doorPoint;
		[SerializeField] [Tooltip("Start point for the letter slide under the door.")]
		private Transform _letterThrowStartPoint;
		[SerializeField] [Tooltip("End point where the letter comes to rest on the floor.")]
		private Transform _letterLandingPoint;
		[SerializeField] [Tooltip("Optional parent for the spawned night letter object.")]
		private Transform _letterParent;
		[SerializeField] [Tooltip("Optional world-space start button used on the 3D main menu.")]
		private MainMenuStartInteractableView _mainMenuStartButton;
		[SerializeField] [Tooltip("Optional root object for main-menu start visuals (for example pole + sign group).")]
		private MainMenuStartVisualRootView _mainMenuStartVisualRoot;
		
		[SerializeField]
		private List<Transform> _letterPoses = new List<Transform>();

		public List<Transform> LetterPoses => _letterPoses;
		public UnityScene Scene { get; private set; }
		public StationActionButtonInteractableView ActionButton => _stationActionButton;
		public StationDecisionToggleInteractableView DecisionToggle => _stationDecisionToggle;
		public StationInspectionInfoView InspectionInfoView => _stationInspectionInfo;
		public BuildingDoorInteractableView BuildingDoor => _buildingDoor;
		public MainMenuStartInteractableView MainMenuStartButton => _mainMenuStartButton;
		public MainMenuStartVisualRootView MainMenuStartVisualRoot => _mainMenuStartVisualRoot;

		public SceneRuntimeSetup BuildRuntimeSetup()
		{
			return new SceneRuntimeSetup(
				_spawnPoint,
				_spawnParent,
				_arrivalDistance,
				_departureDistance,
				_arrivalDurationSeconds,
				_departureDurationSeconds,
				new NightShiftRoomSetup(
					_wakePoint,
					_doorPoint,
					_letterThrowStartPoint,
					_letterLandingPoint,
					_letterParent,
					_letterPoses));
		}

		private void Awake()
		{
			Scene = gameObject.scene;
		}
	}
}
