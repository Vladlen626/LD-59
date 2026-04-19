using System;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Infrastructure.SimpleTweens;
using PlatformCore.Services;
using PlatformCore.Services.Audio;
using Project.Gameplay.Scene;
using Project.Gameplay.Station.Runtime;
using Project.Infrastructure;
using UnityEngine;

namespace Project.Gameplay.Train
{
	public sealed class TrainMovementController : IBaseController, IActivatable, IDeactivatable
	{
		private const float TravelDistanceMultiplier = 5f;
		private const float TravelDurationMultiplier = 5f;
		private const float ArrivalShakeBrakeFadeStart = 0.75f;
		private const float ArrivalShakeRampEnd = 0.5f;
		private const float DepartureShakeRampEnd = 0.25f;
		private const float DepartureShakeFadeStart = 0.5f;
		private const float ArrivalShakeMaxIntensity = 0.55f;
		private const float ArrivalShakeStepDuration = 0.12f;

		private readonly TrainSpawnController _trainSpawnController;
		private readonly StationRuntimeModel _runtimeModel;
		private readonly SceneRuntimeSetup _sceneSetup;
		private readonly ICameraShakeService _cameraShakeService;
		private readonly IAudioService _audioService;
		private readonly ILoggerService _loggerService;

		private bool _isActive;
		private int _movementRevision;

		public TrainMovementController(
			TrainSpawnController trainSpawnController,
			StationSharedRuntimeContext runtimeContext,
			SceneRuntimeSetup sceneSetup,
			ICameraShakeService cameraShakeService,
			IAudioService audioService,
			ILoggerService loggerService)
		{
			_trainSpawnController = trainSpawnController;
			_runtimeModel = runtimeContext.RuntimeModel;
			_sceneSetup = sceneSetup;
			_cameraShakeService = cameraShakeService;
			_audioService = audioService;
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
					PlayMovementShakeAsync(duration, revision, trainView, EvaluateArrivalShakeIntensity),
					PlayArrivalStoppingSoundAsync(duration, revision, trainView));
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
				await UniTask.WhenAll(
					UniTask.Delay(TimeSpan.FromSeconds(duration)),
					PlayMovementShakeAsync(duration, revision, trainView, EvaluateDepartureShakeIntensity));
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

		private async UniTask PlayMovementShakeAsync(
			float duration,
			int revision,
			TrainView trainView,
			Func<float, float> intensityEvaluator)
		{
			if (_cameraShakeService == null || duration <= 0f)
			{
				return;
			}

			if (IsMoveValid(revision, trainView) == false)
			{
				_cameraShakeService.StopShake();
				return;
			}

			var elapsed = 0f;
			while (elapsed < duration)
			{
				if (IsMoveValid(revision, trainView) == false)
				{
					_cameraShakeService.StopShake();
					return;
				}

				var movementProgress = Mathf.Clamp01(elapsed / duration);
				var intensity = intensityEvaluator(movementProgress);
				if (intensity <= 0f)
				{
					var idleStepDuration = Mathf.Min(ArrivalShakeStepDuration, duration - elapsed);
					await UniTask.Delay(TimeSpan.FromSeconds(idleStepDuration));
					elapsed += idleStepDuration;
					continue;
				}

				var stepDuration = Mathf.Min(ArrivalShakeStepDuration, duration - elapsed);
				await _cameraShakeService.ShakeAsync(intensity, stepDuration);
				elapsed += stepDuration;
			}

			_cameraShakeService.StopShake();
		}

		private async UniTask PlayArrivalStoppingSoundAsync(float duration, int revision, TrainView trainView)
		{
			if (_audioService == null || duration <= 0f)
			{
				return;
			}

			var brakeDelay = duration * ArrivalShakeBrakeFadeStart;
			if (brakeDelay > 0f)
			{
				await UniTask.Delay(TimeSpan.FromSeconds(brakeDelay));
			}

			if (IsMoveValid(revision, trainView) == false)
			{
				return;
			}

			_audioService.PlaySoundAt(ProjectAudioEventPaths.TrainStopping, trainView.transform.position);
		}

		private static float EvaluateArrivalShakeIntensity(float arrivalProgress)
		{
			if (arrivalProgress < ArrivalShakeRampEnd)
			{
				var rampProgress = Mathf.InverseLerp(0f, ArrivalShakeRampEnd, arrivalProgress);
				return ArrivalShakeMaxIntensity * Mathf.SmoothStep(0f, 1f, rampProgress);
			}

			if (arrivalProgress < ArrivalShakeBrakeFadeStart)
			{
				return ArrivalShakeMaxIntensity;
			}

			var fadeProgress = Mathf.InverseLerp(ArrivalShakeBrakeFadeStart, 1f, arrivalProgress);
			var fadeFactor = 1f - Mathf.SmoothStep(0f, 1f, fadeProgress);
			return ArrivalShakeMaxIntensity * fadeFactor;
		}

		private static float EvaluateDepartureShakeIntensity(float departureProgress)
		{
			if (departureProgress < DepartureShakeRampEnd)
			{
				var rampProgress = Mathf.InverseLerp(0f, DepartureShakeRampEnd, departureProgress);
				return ArrivalShakeMaxIntensity * Mathf.SmoothStep(0f, 1f, rampProgress);
			}

			if (departureProgress < DepartureShakeFadeStart)
			{
				return ArrivalShakeMaxIntensity;
			}

			var fadeProgress = Mathf.InverseLerp(DepartureShakeFadeStart, 1f, departureProgress);
			var fadeFactor = 1f - Mathf.SmoothStep(0f, 1f, fadeProgress);
			return ArrivalShakeMaxIntensity * fadeFactor;
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
