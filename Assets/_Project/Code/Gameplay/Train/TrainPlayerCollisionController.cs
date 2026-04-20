using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Services;
using Project.Gameplay.Player;
using Project.Gameplay.Station.Controllers;
using UnityEngine;

namespace Project.Gameplay.Train
{
	public sealed class TrainPlayerCollisionController : IBaseController, IActivatable, IDeactivatable, IUpdatable
	{
		private const float LethalSpeedThreshold = 7.5f;
		private const float TrainHazardPadding = 0.35f;
		private const float PlayerFallbackRadius = 0.45f;
		private const float PlayerFallbackHeight = 1.8f;
		private const string TrainDeathNote = "You were struck by the train. The shift is lost.";

		private readonly SamplePlayerView _playerView;
		private readonly TrainSpawnController _trainSpawnController;
		private readonly StationFlowController _stationFlowController;
		private readonly ILoggerService _loggerService;

		private bool _isActive;
		private bool _hasTrainSample;
		private bool _hasTriggeredDeath;
		private Vector3 _lastTrainPosition;

		public TrainPlayerCollisionController(
			SamplePlayerView playerView,
			TrainSpawnController trainSpawnController,
			StationFlowController stationFlowController,
			ILoggerService loggerService)
		{
			_playerView = playerView;
			_trainSpawnController = trainSpawnController;
			_stationFlowController = stationFlowController;
			_loggerService = loggerService;
		}

		public void Activate()
		{
			if (_isActive)
			{
				return;
			}

			_isActive = true;
			_trainSpawnController.CurrentTrainChanged += OnCurrentTrainChanged;
			ResetTrainSample(_trainSpawnController.CurrentTrain);
		}

		public void Deactivate()
		{
			if (_isActive == false)
			{
				return;
			}

			_trainSpawnController.CurrentTrainChanged -= OnCurrentTrainChanged;
			_isActive = false;
			_hasTrainSample = false;
			_hasTriggeredDeath = false;
		}

		public void OnUpdate(float deltaTime)
		{
			if (_isActive == false || _hasTriggeredDeath || !_playerView)
			{
				return;
			}

			var trainView = _trainSpawnController.CurrentTrain;
			if (!trainView)
			{
				_hasTrainSample = false;
				return;
			}

			var currentTrainPosition = trainView.transform.position;
			if (_hasTrainSample == false || deltaTime <= 0f)
			{
				_lastTrainPosition = currentTrainPosition;
				_hasTrainSample = true;
				return;
			}

			var trainDelta = currentTrainPosition - _lastTrainPosition;
			var speed = trainDelta.magnitude / deltaTime;
			if (speed < LethalSpeedThreshold)
			{
				_lastTrainPosition = currentTrainPosition;
				return;
			}

			if (IsPlayerInsideMovingTrain(trainView, trainDelta) == false)
			{
				_lastTrainPosition = currentTrainPosition;
				return;
			}

			_hasTriggeredDeath = true;
			_loggerService?.LogWarning(
				$"[TrainPlayerCollision] Player hit by moving train. Speed={speed:0.00}.");
			_stationFlowController.TryFailCurrentDay(TrainDeathNote);
		}

		private void OnCurrentTrainChanged(TrainView currentTrain)
		{
			ResetTrainSample(currentTrain);
		}

		private void ResetTrainSample(TrainView trainView)
		{
			_hasTriggeredDeath = false;
			_hasTrainSample = false;
			if (!trainView)
			{
				return;
			}

			_lastTrainPosition = trainView.transform.position;
			_hasTrainSample = true;
		}

		private bool IsPlayerInsideMovingTrain(TrainView trainView, Vector3 trainDelta)
		{
			if (TryBuildTrainBounds(trainView, trainDelta, out var trainBounds) == false)
			{
				return false;
			}

			var playerBounds = ResolvePlayerBounds();
			return trainBounds.Intersects(playerBounds);
		}

		private static bool TryBuildTrainBounds(TrainView trainView, Vector3 trainDelta, out Bounds bounds)
		{
			bounds = default;
			var hasBounds = false;

			var colliders = trainView.GetComponentsInChildren<Collider>(true);
			for (var i = 0; i < colliders.Length; i++)
			{
				var collider = colliders[i];
				if (!collider || collider.enabled == false || collider.isTrigger)
				{
					continue;
				}

				Encapsulate(ref bounds, ref hasBounds, collider.bounds);
			}

			if (hasBounds == false)
			{
				var renderers = trainView.GetComponentsInChildren<Renderer>(true);
				for (var i = 0; i < renderers.Length; i++)
				{
					var renderer = renderers[i];
					if (!renderer || renderer.enabled == false)
					{
						continue;
					}

					Encapsulate(ref bounds, ref hasBounds, renderer.bounds);
				}
			}

			if (hasBounds == false)
			{
				return false;
			}

			if (trainDelta.sqrMagnitude > 0.0001f)
			{
				var previousBounds = bounds;
				previousBounds.center -= trainDelta;
				bounds.Encapsulate(previousBounds.min);
				bounds.Encapsulate(previousBounds.max);
			}

			bounds.Expand(TrainHazardPadding);
			return true;
		}

		private Bounds ResolvePlayerBounds()
		{
			var characterController = _playerView.CharacterController;
			if (characterController)
			{
				return characterController.bounds;
			}

			var playerPosition = _playerView.transform.position;
			return new Bounds(
				playerPosition + Vector3.up * (PlayerFallbackHeight * 0.5f),
				new Vector3(
					PlayerFallbackRadius * 2f,
					PlayerFallbackHeight,
					PlayerFallbackRadius * 2f));
		}

		private static void Encapsulate(ref Bounds target, ref bool hasBounds, Bounds value)
		{
			if (hasBounds == false)
			{
				target = value;
				hasBounds = true;
				return;
			}

			target.Encapsulate(value.min);
			target.Encapsulate(value.max);
		}
	}
}
