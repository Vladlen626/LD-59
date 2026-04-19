using System;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using Project.Gameplay.Scene;
using PlatformCore.Services;
using PlatformCore.Services.Factory;
using UnityEngine;

namespace Project.Gameplay.Train
{
	public sealed class TrainSpawnController : IBaseController, IActivatable, IDeactivatable
	{
		private const float ArrivalSpawnDistanceMultiplier = 5f;

		private readonly IObjectFactory _objectFactory;
		private readonly ILoggerService _loggerService;
		private readonly string _trainResourcePath;
		private readonly Transform _spawnPoint;
		private readonly Transform _spawnParent;
		private readonly SceneRuntimeSetup _sceneSetup;

		private TrainView _currentTrain;
		private bool _isActive;
		private int _spawnRevision;

		public event Action<TrainView> CurrentTrainChanged;

		public TrainSpawnController(
			IObjectFactory objectFactory,
			ILoggerService loggerService,
			string trainResourcePath,
			SceneRuntimeSetup sceneSetup,
			Transform spawnPoint,
			Transform spawnParent)
		{
			_objectFactory = objectFactory;
			_loggerService = loggerService;
			_trainResourcePath = trainResourcePath;
			_sceneSetup = sceneSetup;
			_spawnPoint = spawnPoint;
			_spawnParent = spawnParent;
		}

		public TrainView CurrentTrain => _currentTrain;

		public void Activate()
		{
			_isActive = true;
		}

		public void Deactivate()
		{
			_isActive = false;
			ClearCurrentTrain();
		}

		public async UniTask<TrainView> SpawnCurrentTrainAsync()
		{
			if (_isActive == false)
			{
				return null;
			}

			var revision = ++_spawnRevision;
			ClearCurrentTrainInternal();

			var spawnPosition = ResolveArrivalStartPosition();
			var spawnRotation = _spawnPoint ? _spawnPoint.rotation : Quaternion.identity;
			var trainView = await _objectFactory.CreateAsync<TrainView>(
				_trainResourcePath,
				spawnPosition,
				spawnRotation,
				_spawnParent);

			if (revision != _spawnRevision || _isActive == false)
			{
				if (trainView)
				{
					_objectFactory.Destroy(trainView.gameObject);
				}

				return null;
			}

			if (!trainView)
			{
				_loggerService?.LogError($"[TrainSpawn] Failed to spawn TrainView at path '{_trainResourcePath}'.");
				return null;
			}

			_currentTrain = trainView;
			CurrentTrainChanged?.Invoke(_currentTrain);
			return _currentTrain;
		}

		private Vector3 ResolveArrivalStartPosition()
		{
			if (!_spawnPoint)
			{
				return Vector3.zero;
			}

			var direction = _spawnPoint.forward.sqrMagnitude > 0.001f
				? _spawnPoint.forward.normalized
				: Vector3.forward;
			var arrivalDistance = Mathf.Max(0f, _sceneSetup.TrainArrivalDistance) * ArrivalSpawnDistanceMultiplier;
			return _spawnPoint.position - direction * arrivalDistance;
		}

		public void ClearCurrentTrain()
		{
			_spawnRevision++;
			ClearCurrentTrainInternal();
		}

		private void ClearCurrentTrainInternal()
		{
			if (!_currentTrain)
			{
				return;
			}

			_objectFactory.Destroy(_currentTrain.gameObject);
			_currentTrain = null;
			CurrentTrainChanged?.Invoke(null);
		}
	}
}
