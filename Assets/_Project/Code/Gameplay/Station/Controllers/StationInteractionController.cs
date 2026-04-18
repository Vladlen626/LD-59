using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Services;
using PlatformCore.Services.Input;
using PlatformCore.Services.UI;
using Project.Gameplay.Scene;
using Project.Gameplay.Station.Domain;
using Project.Gameplay.Station.Views;
using Project.Gameplay.Train;
using UnityEngine;

namespace Project.Gameplay.Station.Controllers
{
	public sealed class StationInteractionController : IBaseController, IActivatable, IDeactivatable, IUpdatable
	{
		private readonly SceneContext _sceneContext;
		private readonly TrainSpawnController _trainSpawnController;
		private readonly StationFlowController _stationFlowController;
		private readonly IInputService _inputService;
		private readonly ICursorService _cursorService;
		private readonly ILoggerService _loggerService;

		private WagonDoorInteractableView _currentDoorInteractable;
		private bool _isActive;
		private bool _isDoorOpened;
		private bool _hasPendingDecision;
		private PlayerDecision _pendingDecision;

		public StationInteractionController(
			SceneContext sceneContext,
			TrainSpawnController trainSpawnController,
			StationFlowController stationFlowController,
			IInputService inputService,
			ICursorService cursorService,
			ILoggerService loggerService)
		{
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
			ResetDecisionSelection();
			_isActive = false;
		}

		public void OnUpdate(float deltaTime)
		{
			if (_isActive == false)
			{
				return;
			}

			if (_cursorService.IsCursorLocked == false || _inputService.IsInteractPressedThisFrame == false)
			{
				return;
			}

			if (TryGetRaycastInteractable(out var interactable) == false)
			{
				return;
			}

			interactable.Interact();
		}

		private void SubscribeSceneInteractables()
		{
			if (_sceneContext.TrainCallButton)
			{
				_sceneContext.TrainCallButton.Interacted += OnTrainCallInteracted;
			}

			if (_sceneContext.AllowDecisionLever)
			{
				_sceneContext.AllowDecisionLever.Interacted += OnDecisionLeverInteracted;
			}

			if (_sceneContext.RejectDecisionLever)
			{
				_sceneContext.RejectDecisionLever.Interacted += OnDecisionLeverInteracted;
			}

			if (_sceneContext.DispatchButton)
			{
				_sceneContext.DispatchButton.Interacted += OnDispatchButtonInteracted;
			}
		}

		private void UnsubscribeSceneInteractables()
		{
			if (_sceneContext.DispatchButton)
			{
				_sceneContext.DispatchButton.Interacted -= OnDispatchButtonInteracted;
			}

			if (_sceneContext.RejectDecisionLever)
			{
				_sceneContext.RejectDecisionLever.Interacted -= OnDecisionLeverInteracted;
			}

			if (_sceneContext.AllowDecisionLever)
			{
				_sceneContext.AllowDecisionLever.Interacted -= OnDecisionLeverInteracted;
			}

			if (_sceneContext.TrainCallButton)
			{
				_sceneContext.TrainCallButton.Interacted -= OnTrainCallInteracted;
			}
		}

		private bool TryGetRaycastInteractable(out IStationInteractableView interactable)
		{
			interactable = null;

			var rayOrigin = GetRayOrigin();
			if (!rayOrigin)
			{
				return false;
			}

			if (Physics.Raycast(
					rayOrigin.position,
					rayOrigin.forward,
					out var hit,
					_sceneContext.InteractionDistance,
					_sceneContext.InteractionMask,
					QueryTriggerInteraction.Collide) == false)
			{
				return false;
			}

			interactable = hit.collider.GetComponentInParent<IStationInteractableView>();
			return interactable != null;
		}

		private Transform GetRayOrigin()
		{
			if (_sceneContext.InteractionRayOrigin)
			{
				return _sceneContext.InteractionRayOrigin;
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

		private void OnTrainCallInteracted()
		{
			if (_stationFlowController.TryCallNextTrain())
			{
				return;
			}

			_loggerService?.LogWarning(
				"[StationInteraction] Train call ignored: flow is not awaiting train call.");
		}

		private void OnDecisionLeverInteracted(DecisionLeverInteractableView leverView)
		{
			if (CanInteractWithCase() == false || !leverView)
			{
				return;
			}

			_pendingDecision = leverView.Decision;
			_hasPendingDecision = true;
		}

		private void OnDispatchButtonInteracted()
		{
			if (CanInteractWithCase() == false)
			{
				return;
			}

			if (_hasPendingDecision == false)
			{
				_loggerService?.LogWarning("[StationInteraction] Cannot dispatch without selected decision.");
				return;
			}

			if (_stationFlowController.ResolveCase(_pendingDecision, out _))
			{
				ResetDecisionSelection();
			}
		}

		private bool CanInteractWithCase()
		{
			return _stationFlowController.IsAwaitingDecision;
		}

		private void ResetDecisionSelection()
		{
			_hasPendingDecision = false;
			_pendingDecision = PlayerDecision.AllowPassage;
		}
	}
}
