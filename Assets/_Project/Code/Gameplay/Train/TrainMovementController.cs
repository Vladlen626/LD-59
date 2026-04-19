using System;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Infrastructure.SimpleTweens;
using PlatformCore.Services;
using Project.Gameplay.Scene;
using Project.Gameplay.Station.Runtime;
using UnityEngine;

namespace Project.Gameplay.Train
{
	public sealed class TrainMovementController : IBaseController, IActivatable, IDeactivatable
	{
		private const float TravelDistanceMultiplier = 5f;
		private const float TravelDurationMultiplier = 5f;
		private const float ArrivalShakePhaseStart = 0.7f;
		private const int ArrivalShakePulses = 5;
		private const float ArrivalShakeMaxIntensity = 0.45f;

		private readonly TrainSpawnController _trainSpawnController;
		private readonly StationRuntimeModel _runtimeModel;
		private readonly SceneRuntimeSetup _sceneSetup;
		private readonly ICameraShakeService _cameraShakeService;
		private readonly ILoggerService _loggerService;

		private bool _isActive;
		private int _movementRevision;

		public TrainMovementController(
			TrainSpawnController trainSpawnController,
			StationSharedRuntimeContext runtimeContext,
			SceneRuntimeSetup sceneSetup,
			ICameraShakeService cameraShakeService,
			ILoggerService loggerService)
		{
			_trainSpawnController = trainSpawnController;
			_runtimeModel = runtimeContext.RuntimeModel;
			_sceneSetup = sceneSetup;
			_cameraShakeService = cameraShakeService;
			_loggerService = loggerService;
		}

		public void Activate()
		{
			if (_isActive)
			{
				return;
			}

			_isActive = true;
			_runtimeModel.SetTrainTransitState(TrainTransitState.None);
			_trainSpawnController.CurrentTrainChanged += OnCurrentTrainChanged;
		}

		public void Deactivate()
		{
			if (_isActive == false)
			{
				return;
			}

			_trainSpawnController.CurrentTrainChanged -= OnCurrentTrainChanged;
			_runtimeModel.SetTrainTransitState(TrainTransitState.None);
			_cameraShakeService?.StopShake();
			_movementRevision++;
			_isActive = false;
		}

		public async UniTask<bool> PlayArrivalAsync()
		{
			var trainView = _trainSpawnController.CurrentTrain;
			if (_isActive == false || !trainView)
			{
				return false;
			}

			var revision = ++_movementRevision;
			var direction = ResolveDirection();
			var stopWorldPosition = ResolveStopWorldPosition(trainView);
			var arrivalDistance = Mathf.Max(0f, _sceneSetup.TrainArrivalDistance) * TravelDistanceMultiplier;
			var startWorldPosition = stopWorldPosition - direction * arrivalDistance;
			var stopLocalPosition = ToLocalPosition(trainView.transform, stopWorldPosition);
			var startLocalPosition = ToLocalPosition(trainView.transform, startWorldPosition);
			var duration = Mathf.Max(0f, _sceneSetup.TrainArrivalDuration) * TravelDurationMultiplier;

			trainView.transform.localPosition = startLocalPosition;
			_runtimeModel.SetTrainTransitState(TrainTransitState.Arriving);
			_cameraShakeService?.StopShake();

			if (duration > 0f)
			{
				SimpleTweensTransform.MoveToLocalPositionSmoothStop(trainView.transform, stopLocalPosition, duration);
				await UniTask.WhenAll(
					UniTask.Delay(TimeSpan.FromSeconds(duration)),
					PlayArrivalShakeAsync(duration, revision, trainView));
			}

			if (IsMoveValid(revision, trainView) == false)
			{
				return false;
			}

			trainView.transform.localPosition = stopLocalPosition;
			_runtimeModel.SetTrainTransitState(TrainTransitState.AtStation);
			_loggerService?.Log("[TrainMovement] Train arrived at station.");
			return true;
		}

		public async UniTask<bool> PlayDepartureAsync()
		{
			var trainView = _trainSpawnController.CurrentTrain;
			if (_isActive == false || !trainView)
			{
				_runtimeModel.SetTrainTransitState(TrainTransitState.None);
				return false;
			}

			var revision = ++_movementRevision;
			var direction = ResolveDirection();
			var stopWorldPosition = ResolveStopWorldPosition(trainView);
			var departureDistance = Mathf.Max(0f, _sceneSetup.TrainDepartureDistance) * TravelDistanceMultiplier;
			var targetWorldPosition = stopWorldPosition + direction * departureDistance;
			var targetLocalPosition = ToLocalPosition(trainView.transform, targetWorldPosition);
			var duration = Mathf.Max(0f, _sceneSetup.TrainDepartureDuration) * TravelDurationMultiplier;

			_runtimeModel.SetTrainTransitState(TrainTransitState.Departing);
			_cameraShakeService?.StopShake();

			if (duration > 0f)
			{
				SimpleTweensTransform.MoveToLocalPositionSmoothStart(trainView.transform, targetLocalPosition, duration);
				await UniTask.Delay(TimeSpan.FromSeconds(duration));
			}

			if (IsMoveValid(revision, trainView) == false)
			{
				return false;
			}

			trainView.transform.localPosition = targetLocalPosition;
			_runtimeModel.SetTrainTransitState(TrainTransitState.None);
			_trainSpawnController.ClearCurrentTrain();
			_loggerService?.Log("[TrainMovement] Train departed from station.");
			return true;
		}

		private void OnCurrentTrainChanged(TrainView currentTrain)
		{
			if (!currentTrain)
			{
				_cameraShakeService?.StopShake();
				_runtimeModel.SetTrainTransitState(TrainTransitState.None);
			}
		}

		private async UniTask PlayArrivalShakeAsync(float duration, int revision, TrainView trainView)
		{
			if (_cameraShakeService == null || duration <= 0f)
			{
				return;
			}

			var shakeDelay = duration * ArrivalShakePhaseStart;
			if (shakeDelay > 0f)
			{
				await UniTask.Delay(TimeSpan.FromSeconds(shakeDelay));
			}

			if (IsMoveValid(revision, trainView) == false)
			{
				_cameraShakeService.StopShake();
				return;
			}

			var shakeDuration = Mathf.Max(0.01f, duration - shakeDelay);
			var pulseDuration = shakeDuration / ArrivalShakePulses;

			for (var i = 0; i < ArrivalShakePulses; i++)
			{
				if (IsMoveValid(revision, trainView) == false)
				{
					_cameraShakeService.StopShake();
					return;
				}

				var normalizedPulse = (i + 0.5f) / ArrivalShakePulses;
				var intensityFactor = Mathf.Pow(1f - normalizedPulse, 2f);
				var intensity = ArrivalShakeMaxIntensity * intensityFactor;
				await _cameraShakeService.ShakeAsync(intensity, pulseDuration);
			}

			_cameraShakeService.StopShake();
		}

		private Vector3 ResolveDirection()
		{
			if (_sceneSetup.TrainSpawnPoint)
			{
				var direction = _sceneSetup.TrainSpawnPoint.forward;
				if (direction.sqrMagnitude > 0.001f)
				{
					return direction.normalized;
				}
			}

			return Vector3.forward;
		}

		private Vector3 ResolveStopWorldPosition(TrainView trainView)
		{
			return _sceneSetup.TrainSpawnPoint
				? _sceneSetup.TrainSpawnPoint.position
				: trainView.transform.position;
		}

		private static Vector3 ToLocalPosition(Transform target, Vector3 worldPosition)
		{
			var parent = target.parent;
			return parent ? parent.InverseTransformPoint(worldPosition) : worldPosition;
		}

		private bool IsMoveValid(int revision, TrainView trainView)
		{
			return _isActive &&
				revision == _movementRevision &&
				trainView &&
				_trainSpawnController.CurrentTrain == trainView;
		}
	}
}
