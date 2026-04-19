using System;
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
		private const float FeedbackDuration = 1.15f;
		private const string HintCallTrain = "Call Train";
		private const string HintDispatchTrain = "Dispatch Train";
		private const string HintOpenWagonFirst = "Open wagon first";
		private const string HintCloseWagonFirst = "Close wagon first";
		private const string HintOpenWagon = "Open Wagon";
		private const string HintCloseWagon = "Close Wagon";
		private const string HintTrainInMotion = "Train in motion";
		private const string HintDayComplete = "Day complete";
		private const string HintStandBy = "Stand by";
		private const string HintVerdictAllow = "Verdict: Allow\nInteract to switch";
		private const string HintVerdictSendBack = "Verdict: Send Back\nInteract to switch";

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
		private string _currentHint = string.Empty;
		private string _temporaryHint = string.Empty;
		private float _temporaryHintUntil;
		private bool _isActive;
		private PlayerDecision _selectedDecision = PlayerDecision.AllowPassage;

		public event Action<string> HintChanged;

		public string CurrentHint => _currentHint;

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
				_runtimeModel.CurrentWagonDoorStateChanged += OnDoorStateChanged;
			}

			_trainSpawnController.CurrentTrainChanged += OnCurrentTrainChanged;
			OnCurrentTrainChanged(_trainSpawnController.CurrentTrain);
			RefreshHint();
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
				_runtimeModel.CurrentWagonDoorStateChanged -= OnDoorStateChanged;
			}

			_trainSpawnController.CurrentTrainChanged -= OnCurrentTrainChanged;
			UnsubscribeSceneInteractables();
			UnbindCurrentDoorInteractable();
			_hoveredInteractable = null;
			_rayOrigin = null;
			_isActive = false;
			ClearTemporaryHint();
			SetCurrentHint(string.Empty);
		}

		public void OnUpdate(float _)
		{
			if (_isActive == false)
			{
				return;
			}

			UpdateHoveredInteractable(ResolveCurrentHoveredInteractable());
			if (_playerView && _playerView.gameObject.activeInHierarchy == false)
			{
				return;
			}

			if (_cursorService.IsCursorLocked == false || _inputService.IsInteractPressedThisFrame == false)
			{
				return;
			}

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

		private IPlayerInteractable ResolveCurrentHoveredInteractable()
		{
			if (_cursorService.IsCursorLocked == false)
			{
				return null;
			}

			if (_playerView && _playerView.gameObject.activeInHierarchy == false)
			{
				return null;
			}

			if (!_rayOrigin)
			{
				_rayOrigin = ResolveRayOrigin();
				if (!_rayOrigin)
				{
					return null;
				}
			}

			return ResolveHoveredInteractable(_rayOrigin);
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
				InvalidateHintState();
				return;
			}

			var doorInteractable = currentTrain.DoorInteractable;
			if (!doorInteractable)
			{
				_loggerService?.LogWarning("[StationInteraction] WagonDoorInteractableView is missing on TrainView.");
				InvalidateHintState();
				return;
			}

			_currentDoorInteractable = doorInteractable;
			_currentDoorInteractable.Interacted += OnDoorInteracted;
			_currentDoorInteractable.SetOpenState(_runtimeModel != null && _runtimeModel.IsCurrentWagonDoorOpen);
			InvalidateHintState();
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
			_runtimeModel?.SetCurrentWagonDoorOpen(false);
		}

		private void OnDoorInteracted()
		{
			if (_runtimeModel.IsTrainInTransit || CanInteractWithCase() == false || !_currentDoorInteractable)
			{
				return;
			}

			var shouldOpenDoor = _runtimeModel.IsCurrentWagonDoorOpen == false;
			_runtimeModel.SetCurrentWagonDoorOpen(shouldOpenDoor);

			if (shouldOpenDoor)
			{
				_runtimeModel.MarkCurrentWagonInspected();
			}
		}

		private void OnActionButtonInteracted()
		{
			if (_runtimeModel.IsTrainInTransit)
			{
				ShowActionFeedback(HintTrainInMotion);
				return;
			}

			if (_stationFlowController.IsAwaitingDecision)
			{
				if (_runtimeModel.HasInspectedCurrentWagon == false)
				{
					ShowActionFeedback(HintOpenWagonFirst);
					return;
				}

				if (_runtimeModel.IsCurrentWagonDoorOpen)
				{
					ShowActionFeedback(HintCloseWagonFirst);
					return;
				}

				if (_stationFlowController.ResolveCase(_selectedDecision, out _))
				{
					ResetDecisionSelection();
				}

				InvalidateHintState();
				return;
			}

			if (_stationFlowController.TryCallNextTrain() == false)
			{
				ShowActionFeedback(_runtimeModel.FlowState == StationFlowState.FinishDay ? HintDayComplete : HintStandBy);
				return;
			}

			InvalidateHintState();
		}

		private void OnDecisionToggleInteracted(PlayerDecision decision)
		{
			if (_runtimeModel.IsTrainInTransit)
			{
				return;
			}

			_selectedDecision = decision;
			if (_sceneContext.DecisionToggle)
			{
				_sceneContext.DecisionToggle.SetDecision(_selectedDecision);
			}

			InvalidateHintState();
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

		private void OnRuntimeScenarioChanged(TrainScenario _)
		{
			InvalidateHintState();
		}

		private void OnFlowStateChanged(StationFlowState _)
		{
			InvalidateHintState();
		}

		private void OnTrainTransitStateChanged(TrainTransitState _)
		{
			InvalidateHintState();
		}

		private void OnInspectionStateChanged(bool _)
		{
			InvalidateHintState();
		}

		private void OnDoorStateChanged(bool isOpen)
		{
			if (_currentDoorInteractable)
			{
				_currentDoorInteractable.SetOpenState(isOpen);
			}

			InvalidateHintState();
		}

		private void InvalidateHintState()
		{
			ClearTemporaryHint();
			RefreshHint();
		}

		private void UpdateHoveredInteractable(IPlayerInteractable hoveredInteractable)
		{
			if (ReferenceEquals(_hoveredInteractable, hoveredInteractable))
			{
				RefreshHint();
				return;
			}

			_hoveredInteractable = hoveredInteractable;
			RefreshHint();
		}

		private void RefreshHint()
		{
			if (_isActive == false)
			{
				SetCurrentHint(string.Empty);
				return;
			}

			if (TryGetTemporaryHint(out var temporaryHint))
			{
				SetCurrentHint(temporaryHint);
				return;
			}

			SetCurrentHint(ResolveHint(_hoveredInteractable));
		}

		private string ResolveHint(IPlayerInteractable interactable)
		{
			if (interactable == null)
			{
				return string.Empty;
			}

			if (ReferenceEquals(interactable, _sceneContext.ActionButton))
			{
				return ResolveActionButtonHint();
			}

			if (ReferenceEquals(interactable, _sceneContext.DecisionToggle))
			{
				return ResolveDecisionToggleHint();
			}

			if (ReferenceEquals(interactable, _currentDoorInteractable))
			{
				return ResolveDoorHint();
			}

			return string.Empty;
		}

		private string ResolveActionButtonHint()
		{
			if (_runtimeModel.IsTrainInTransit)
			{
				return HintTrainInMotion;
			}

			if (_stationFlowController.IsAwaitingDecision)
			{
				if (_runtimeModel.HasInspectedCurrentWagon == false)
				{
					return HintOpenWagonFirst;
				}

				return _runtimeModel.IsCurrentWagonDoorOpen
					? HintCloseWagonFirst
					: HintDispatchTrain;
			}

			if (_stationFlowController.IsAwaitingTrainCall)
			{
				return HintCallTrain;
			}

			if (_runtimeModel.FlowState == StationFlowState.FinishDay)
			{
				return HintDayComplete;
			}

			return HintStandBy;
		}

		private string ResolveDecisionToggleHint()
		{
			if (_stationFlowController.IsAwaitingDecision == false || _runtimeModel.IsTrainInTransit)
			{
				return string.Empty;
			}

			return _selectedDecision == PlayerDecision.AllowPassage
				? HintVerdictAllow
				: HintVerdictSendBack;
		}

		private string ResolveDoorHint()
		{
			if (CanInspectCurrentWagon() == false)
			{
				return string.Empty;
			}

			return _runtimeModel.IsCurrentWagonDoorOpen ? HintCloseWagon : HintOpenWagon;
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
			if (string.IsNullOrWhiteSpace(message))
			{
				return;
			}

			_temporaryHint = message;
			_temporaryHintUntil = Time.unscaledTime + FeedbackDuration;
			RefreshHint();
		}

		private bool TryGetTemporaryHint(out string hint)
		{
			if (string.IsNullOrEmpty(_temporaryHint))
			{
				hint = string.Empty;
				return false;
			}

			if (Time.unscaledTime >= _temporaryHintUntil)
			{
				ClearTemporaryHint();
				hint = string.Empty;
				return false;
			}

			hint = _temporaryHint;
			return true;
		}

		private void ClearTemporaryHint()
		{
			_temporaryHint = string.Empty;
			_temporaryHintUntil = 0f;
		}

		private void SetCurrentHint(string hint)
		{
			var normalizedHint = string.IsNullOrWhiteSpace(hint) ? string.Empty : hint;
			if (string.Equals(_currentHint, normalizedHint, StringComparison.Ordinal))
			{
				return;
			}

			_currentHint = normalizedHint;
			HintChanged?.Invoke(_currentHint);
		}
	}
}
