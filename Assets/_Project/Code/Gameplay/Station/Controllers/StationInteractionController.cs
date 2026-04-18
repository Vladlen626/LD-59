using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Services;
using PlatformCore.Services.Input;
using PlatformCore.Services.UI;
using Project.Gameplay.Interaction;
using Project.Gameplay.Player;
using Project.Gameplay.Scene;
using Project.Gameplay.Station.Domain;
using Project.Gameplay.Station.Runtime;
using Project.Gameplay.Station.Views;
using Project.Gameplay.Train;
using UnityEngine;

namespace Project.Gameplay.Station.Controllers
{
	public sealed class StationInteractionController : IBaseController, IActivatable, IDeactivatable, IUpdatable
	{
		private const int MaxRaycastHits = 16;
		private const int MaxOverlapHits = 32;
		private const float DefaultInteractionDistance = 4f;
		private const int DefaultInteractionMask = ~0;

		private readonly SamplePlayerView _playerView;
		private readonly SceneContext _sceneContext;
		private readonly StationRuntimeModel _runtimeModel;
		private readonly TrainSpawnController _trainSpawnController;
		private readonly StationFlowController _stationFlowController;
		private readonly IInputService _inputService;
		private readonly ICursorService _cursorService;
		private readonly ILoggerService _loggerService;
		private readonly RaycastHit[] _raycastHits = new RaycastHit[MaxRaycastHits];
		private readonly Collider[] _overlapColliders = new Collider[MaxOverlapHits];

		private WagonDoorInteractableView _currentDoorInteractable;
		private IPlayerInteractable _hoveredInteractable;
		private Transform _rayOrigin;
		private bool _isActive;
		private bool _isDoorOpened;
		private PlayerDecision _selectedDecision = PlayerDecision.AllowPassage;

		public StationInteractionController(
			SamplePlayerView playerView,
			SceneContext sceneContext,
			StationSharedRuntimeContext runtimeContext,
			TrainSpawnController trainSpawnController,
			StationFlowController stationFlowController,
			IInputService inputService,
			ICursorService cursorService,
			ILoggerService loggerService)
		{
			_playerView = playerView;
			_sceneContext = sceneContext;
			_runtimeModel = runtimeContext.RuntimeModel;
			_trainSpawnController = trainSpawnController;
			_stationFlowController = stationFlowController;
			_inputService = inputService;
			_cursorService = cursorService;
			_loggerService = loggerService;
		}

		public void Activate()
		{
			if (_isActive)
			{
				return;
			}

			_isActive = true;
			_rayOrigin = ResolveRayOrigin();
			SubscribeSceneInteractables();

			if (_runtimeModel != null)
			{
				_runtimeModel.ActiveScenarioChanged += OnRuntimeScenarioChanged;
				_runtimeModel.FlowStateChanged += OnFlowStateChanged;
				_runtimeModel.TrainTransitStateChanged += OnTrainTransitStateChanged;
				_runtimeModel.CurrentWagonInspectionChanged += OnInspectionStateChanged;
			}

			_trainSpawnController.CurrentTrainChanged += OnCurrentTrainChanged;
			OnCurrentTrainChanged(_trainSpawnController.CurrentTrain);
			RefreshInteractableHints();
		}

		public void Deactivate()
		{
			if (_isActive == false)
			{
				return;
			}

			if (_runtimeModel != null)
			{
				_runtimeModel.ActiveScenarioChanged -= OnRuntimeScenarioChanged;
				_runtimeModel.FlowStateChanged -= OnFlowStateChanged;
				_runtimeModel.TrainTransitStateChanged -= OnTrainTransitStateChanged;
				_runtimeModel.CurrentWagonInspectionChanged -= OnInspectionStateChanged;
			}

			_trainSpawnController.CurrentTrainChanged -= OnCurrentTrainChanged;
			UnsubscribeSceneInteractables();
			UnbindCurrentDoorInteractable();
			_hoveredInteractable = null;
			_rayOrigin = null;
			_isActive = false;
		}

		public void OnUpdate(float _)
		{
			if (_isActive == false)
			{
				return;
			}

			if (_runtimeModel.IsTrainInTransit)
			{
				_hoveredInteractable = null;
				return;
			}

			if (_cursorService.IsCursorLocked == false || _inputService.IsInteractPressedThisFrame == false)
			{
				return;
			}

			if (_playerView && _playerView.gameObject.activeInHierarchy == false)
			{
				_hoveredInteractable = null;
				return;
			}

			if (!_rayOrigin)
			{
				_rayOrigin = ResolveRayOrigin();
				if (!_rayOrigin)
				{
					return;
				}
			}

			_hoveredInteractable = ResolveHoveredInteractable(_rayOrigin);
			(_hoveredInteractable ?? ResolveNearestInteractable())?.Interact();
		}

		private void SubscribeSceneInteractables()
		{
			if (_sceneContext.ActionButton)
			{
				_sceneContext.ActionButton.Interacted += OnActionButtonInteracted;
			}

			if (_sceneContext.DecisionToggle)
			{
				_sceneContext.DecisionToggle.Interacted += OnDecisionToggleInteracted;
			}
		}

		private void UnsubscribeSceneInteractables()
		{
			if (_sceneContext.DecisionToggle)
			{
				_sceneContext.DecisionToggle.Interacted -= OnDecisionToggleInteracted;
			}

			if (_sceneContext.ActionButton)
			{
				_sceneContext.ActionButton.Interacted -= OnActionButtonInteracted;
			}
		}

		private IPlayerInteractable ResolveHoveredInteractable(Transform rayOrigin)
		{
			var interactionDistance = GetInteractionDistance();
			var interactionMask = GetInteractionMask();
			var hitCount = Physics.RaycastNonAlloc(
				rayOrigin.position,
				rayOrigin.forward,
				_raycastHits,
				interactionDistance,
				interactionMask,
				QueryTriggerInteraction.Collide);

			IPlayerInteractable selected = null;
			var selectedDistance = float.MaxValue;
			var selectedPriority = int.MinValue;

			for (var i = 0; i < hitCount; i++)
			{
				var hit = _raycastHits[i];
				if (PlayerInteractableRegistry.TryGet(hit.collider, out var interactable) == false || interactable.CanInteract == false)
				{
					continue;
				}

				var isFurther = hit.distance > selectedDistance + 0.001f;
				var sameDistanceLowerPriority = Mathf.Abs(hit.distance - selectedDistance) <= 0.001f &&
					interactable.Priority <= selectedPriority;
				if (isFurther || sameDistanceLowerPriority)
				{
					continue;
				}

				selected = interactable;
				selectedDistance = hit.distance;
				selectedPriority = interactable.Priority;
			}

			return selected;
		}

		private IPlayerInteractable ResolveNearestInteractable()
		{
			if (!_rayOrigin)
			{
				return null;
			}

			var sourcePosition = _playerView ? _playerView.transform.position : _rayOrigin.position;
			var interactionDistance = GetInteractionDistance();
			var interactionMask = GetInteractionMask();
			var hitCount = Physics.OverlapSphereNonAlloc(
				sourcePosition,
				interactionDistance,
				_overlapColliders,
				interactionMask,
				QueryTriggerInteraction.Collide);

			IPlayerInteractable selected = null;
			var selectedDistance = float.MaxValue;
			var selectedPriority = int.MinValue;

			for (var i = 0; i < hitCount; i++)
			{
				var collider = _overlapColliders[i];
				if (PlayerInteractableRegistry.TryGet(collider, out var interactable) == false || interactable.CanInteract == false)
				{
					continue;
				}

				var point = collider.ClosestPoint(sourcePosition);
				var distance = (point - sourcePosition).sqrMagnitude;
				var isFurther = distance > selectedDistance + 0.001f;
				var sameDistanceLowerPriority = Mathf.Abs(distance - selectedDistance) <= 0.001f &&
					interactable.Priority <= selectedPriority;
				if (isFurther || sameDistanceLowerPriority)
				{
					continue;
				}

				selected = interactable;
				selectedDistance = distance;
				selectedPriority = interactable.Priority;
			}

			return selected;
		}

		private Transform ResolveRayOrigin()
		{
			if (_playerView)
			{
				if (_playerView.InteractionRayOrigin)
				{
					return _playerView.InteractionRayOrigin;
				}

				if (_playerView.CameraRoot)
				{
					return _playerView.CameraRoot;
				}

				if (_playerView.Head)
				{
					return _playerView.Head;
				}

				return _playerView.transform;
			}

			var mainCamera = Camera.main;
			return mainCamera ? mainCamera.transform : null;
		}

		private float GetInteractionDistance()
		{
			return _playerView ? _playerView.InteractionDistance : DefaultInteractionDistance;
		}

		private int GetInteractionMask()
		{
			return _playerView ? _playerView.InteractionMask : DefaultInteractionMask;
		}

		private void OnCurrentTrainChanged(TrainView currentTrain)
		{
			UnbindCurrentDoorInteractable();
			ResetDecisionSelection();

			if (!currentTrain)
			{
				return;
			}

			var doorInteractable = currentTrain.DoorInteractable;
			if (!doorInteractable)
			{
				_loggerService?.LogWarning("[StationInteraction] WagonDoorInteractableView is missing on TrainView.");
				return;
			}

			_currentDoorInteractable = doorInteractable;
			_currentDoorInteractable.Interacted += OnDoorInteracted;
			_currentDoorInteractable.SetOpenState(false);
			_currentDoorInteractable.SetHintVisible(CanInspectCurrentWagon());
			_isDoorOpened = false;
			RefreshInteractableHints();
		}

		private void UnbindCurrentDoorInteractable()
		{
			if (!_currentDoorInteractable)
			{
				return;
			}

			_currentDoorInteractable.Interacted -= OnDoorInteracted;
			_currentDoorInteractable.SetOpenState(false);
			_currentDoorInteractable.SetHintVisible(false);
			_currentDoorInteractable = null;
			_isDoorOpened = false;
		}

		private void OnDoorInteracted()
		{
			if (_runtimeModel.IsTrainInTransit || CanInteractWithCase() == false || !_currentDoorInteractable)
			{
				return;
			}

			_isDoorOpened = !_isDoorOpened;
			_currentDoorInteractable.SetOpenState(_isDoorOpened);

			if (_isDoorOpened)
			{
				_runtimeModel.MarkCurrentWagonInspected();
			}

			RefreshInteractableHints();
		}

		private void OnActionButtonInteracted()
		{
			if (_runtimeModel.IsTrainInTransit)
			{
				ShowActionFeedback("Train in motion");
				return;
			}

			if (_stationFlowController.IsAwaitingDecision)
			{
				if (_runtimeModel.HasInspectedCurrentWagon == false)
				{
					ShowActionFeedback("Open wagon first");
					RefreshInteractableHints();
					return;
				}

				if (_stationFlowController.ResolveCase(_selectedDecision, out _))
				{
					ResetDecisionSelection();
				}

				RefreshInteractableHints();
				return;
			}

			if (_stationFlowController.TryCallNextTrain() == false)
			{
				ShowActionFeedback(_runtimeModel.FlowState == StationFlowState.FinishDay ? "Day complete" : "Stand by");
			}

			RefreshInteractableHints();
		}

		private void OnDecisionToggleInteracted(PlayerDecision decision)
		{
			if (_runtimeModel.IsTrainInTransit)
			{
				return;
			}

			_selectedDecision = decision;
			RefreshInteractableHints();
		}

		private bool CanInteractWithCase()
		{
			return _stationFlowController.IsAwaitingDecision;
		}

		private void ResetDecisionSelection()
		{
			_selectedDecision = PlayerDecision.AllowPassage;

			if (_sceneContext.DecisionToggle)
			{
				_sceneContext.DecisionToggle.SetDecision(_selectedDecision);
			}

			RefreshInteractableHints();
		}

		private void OnRuntimeScenarioChanged(TrainScenario _)
		{
			RefreshInteractableHints();
		}

		private void OnFlowStateChanged(StationFlowState _)
		{
			RefreshInteractableHints();
		}

		private void OnTrainTransitStateChanged(TrainTransitState _)
		{
			RefreshInteractableHints();
		}

		private void OnInspectionStateChanged(bool _)
		{
			RefreshInteractableHints();
		}

		private void RefreshInteractableHints()
		{
			RefreshActionButtonHint();
			RefreshDoorHint();
			RefreshDecisionToggleHint();
		}

		private void RefreshActionButtonHint()
		{
			if (!_sceneContext.ActionButton)
			{
				return;
			}

			if (_runtimeModel.IsTrainInTransit)
			{
				_sceneContext.ActionButton.SetDisplay("Train in motion", StationActionButtonInteractableView.DisplayState.Blocked);
				return;
			}

			if (_stationFlowController.IsAwaitingDecision)
			{
				var needsInspection = _runtimeModel.HasInspectedCurrentWagon == false;
				_sceneContext.ActionButton.SetDisplay(
					needsInspection ? "Open wagon first" : "Dispatch Train",
					needsInspection
						? StationActionButtonInteractableView.DisplayState.Blocked
						: StationActionButtonInteractableView.DisplayState.Ready);
				return;
			}

			if (_stationFlowController.IsAwaitingTrainCall)
			{
				_sceneContext.ActionButton.SetDisplay("Call Train", StationActionButtonInteractableView.DisplayState.Ready);
				return;
			}

			if (_runtimeModel.FlowState == StationFlowState.FinishDay)
			{
				_sceneContext.ActionButton.SetDisplay("Day complete", StationActionButtonInteractableView.DisplayState.Neutral);
				return;
			}

			_sceneContext.ActionButton.SetDisplay("Stand by", StationActionButtonInteractableView.DisplayState.Neutral);
		}

		private void RefreshDoorHint()
		{
			if (_currentDoorInteractable)
			{
				_currentDoorInteractable.SetHintVisible(CanInspectCurrentWagon());
			}
		}

		private void RefreshDecisionToggleHint()
		{
			if (_sceneContext.DecisionToggle)
			{
				_sceneContext.DecisionToggle.SetDecision(_selectedDecision);
			}
		}

		private bool CanInspectCurrentWagon()
		{
			return _isActive &&
				_currentDoorInteractable &&
				_stationFlowController.IsAwaitingDecision &&
				_runtimeModel.IsTrainInTransit == false;
		}

		private void ShowActionFeedback(string message)
		{
			if (_sceneContext.ActionButton)
			{
				_sceneContext.ActionButton.ShowTemporaryFeedback(
					message,
					StationActionButtonInteractableView.DisplayState.Blocked);
			}
		}
	}
}
