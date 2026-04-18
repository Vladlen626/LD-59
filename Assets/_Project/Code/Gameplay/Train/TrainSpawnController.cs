using System;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Services;
using PlatformCore.Services.Factory;
using UnityEngine;

namespace Project.Gameplay.Train
{
	public sealed class TrainSpawnController : IBaseController, IActivatable, IDeactivatable
	{
		private readonly IObjectFactory _objectFactory;
		private readonly ILoggerService _loggerService;
		private readonly string _trainResourcePath;
		private readonly Transform _spawnPoint;
		private readonly Transform _spawnParent;

		private TrainView _currentTrain;
		private bool _isActive;
		private int _spawnRevision;

		public event Action<TrainView> CurrentTrainChanged;

		public TrainSpawnController(
			IObjectFactory objectFactory,
			ILoggerService loggerService,
			string trainResourcePath,
			Transform spawnPoint,
			Transform spawnParent)
		{
			_objectFactory = objectFactory;
			_loggerService = loggerService;
			_trainResourcePath = trainResourcePath;
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

			if (string.IsNullOrWhiteSpace(_trainResourcePath))
			{
				_loggerService?.LogError("[TrainSpawn] Train resource path is empty.");
				return null;
			}

			var revision = ++_spawnRevision;
			ClearCurrentTrainInternal();

			var spawnPosition = _spawnPoint ? _spawnPoint.position : Vector3.zero;
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
