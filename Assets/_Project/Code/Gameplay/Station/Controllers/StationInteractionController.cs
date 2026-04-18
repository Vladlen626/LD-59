using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Services;
using PlatformCore.Services.Input;
using PlatformCore.Services.UI;
using Project.Gameplay.Interaction;
using Project.Gameplay.Player;
using Project.Gameplay.Scene;
using Project.Gameplay.Station.Domain;
using Project.Gameplay.Station.Views;
using Project.Gameplay.Train;
using UnityEngine;

namespace Project.Gameplay.Station.Controllers
{
	public sealed class StationInteractionController : IBaseController, IActivatable, IDeactivatable, IUpdatable
	{
		private const int MaxRaycastHits = 16;
		private const int MaxOverlapHits = 32;

		private readonly SamplePlayerView _playerView;
		private readonly SceneContext _sceneContext;
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
			TrainSpawnController trainSpawnController,
			StationFlowController stationFlowController,
			IInputService inputService,
			ICursorService cursorService,
			ILoggerService loggerService)
		{
			_playerView = playerView;
			_sceneContext = sceneContext;
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
			_trainSpawnController.CurrentTrainChanged += OnCurrentTrainChanged;
			OnCurrentTrainChanged(_trainSpawnController.CurrentTrain);
		}

		public void Deactivate()
		{
			if (_isActive == false)
			{
				return;
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
			var hitCount = Physics.RaycastNonAlloc(
				rayOrigin.position,
				rayOrigin.forward,
				_raycastHits,
				_sceneContext.InteractionDistance,
				_sceneContext.InteractionMask,
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
			var hitCount = Physics.OverlapSphereNonAlloc(
				sourcePosition,
				_sceneContext.InteractionDistance,
				_overlapColliders,
				_sceneContext.InteractionMask,
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
			if (_sceneContext.InteractionRayOrigin)
			{
				return _sceneContext.InteractionRayOrigin;
			}

			if (_playerView)
			{
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
				doorInteractable = currentTrain.GetComponentInChildren<WagonDoorInteractableView>(true);
			}

			if (!doorInteractable)
			{
				_loggerService?.LogWarning("[StationInteraction] WagonDoorInteractableView is missing on TrainView.");
				return;
			}

			_currentDoorInteractable = doorInteractable;
			_currentDoorInteractable.Interacted += OnDoorInteracted;
			_currentDoorInteractable.SetOpenState(false);
			_isDoorOpened = false;
		}

		private void UnbindCurrentDoorInteractable()
		{
			if (!_currentDoorInteractable)
			{
				return;
			}

			_currentDoorInteractable.Interacted -= OnDoorInteracted;
			_currentDoorInteractable.SetOpenState(false);
			_currentDoorInteractable = null;
			_isDoorOpened = false;
		}

		private void OnDoorInteracted()
		{
			if (CanInteractWithCase() == false || !_currentDoorInteractable)
			{
				return;
			}

			_isDoorOpened = !_isDoorOpened;
			_currentDoorInteractable.SetOpenState(_isDoorOpened);
		}

		private void OnActionButtonInteracted()
		{
			if (_stationFlowController.IsAwaitingDecision)
			{
				if (_stationFlowController.ResolveCase(_selectedDecision, out _))
				{
					ResetDecisionSelection();
				}

				return;
			}

			_stationFlowController.TryCallNextTrain();
		}

		private void OnDecisionToggleInteracted(PlayerDecision decision)
		{
			_selectedDecision = decision;
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
		}
	}
}
