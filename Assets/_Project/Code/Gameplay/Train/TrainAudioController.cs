using System;
using System.Reflection;
using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Services;
using PlatformCore.Services.Audio;
using Project.Gameplay.Station.Runtime;
using Project.Infrastructure;
using UnityEngine;

namespace Project.Gameplay.Train
{
	public sealed class TrainAudioController : IBaseController, IActivatable, IDeactivatable
	{
		private const string RuntimeManagerTypeName = "FMODUnity.RuntimeManager";
		private const string EventInstanceTypeName = "FMOD.Studio.EventInstance";
		private const string StopModeTypeName = "FMOD.Studio.STOP_MODE";

		private readonly TrainSpawnController _trainSpawnController;
		private readonly StationRuntimeModel _runtimeModel;
		private readonly ILoggerService _loggerService;

		private object _movementLoopInstance;
		private TrainView _currentTrain;
		private bool _isActive;
		private bool _isMovementLoopPlaying;

		public TrainAudioController(
			TrainSpawnController trainSpawnController,
			StationSharedRuntimeContext runtimeContext,
			ILoggerService loggerService)
		{
			_trainSpawnController = trainSpawnController;
			_runtimeModel = runtimeContext?.RuntimeModel;
			_loggerService = loggerService;
		}

		public void Activate()
		{
			if (_isActive)
			{
				return;
			}

			_isActive = true;
			_currentTrain = _trainSpawnController.CurrentTrain;
			_trainSpawnController.CurrentTrainChanged += OnCurrentTrainChanged;

			if (_runtimeModel != null)
			{
				_runtimeModel.TrainTransitStateChanged += OnTrainTransitStateChanged;
			}

			RefreshMovementLoop();
		}

		public void Deactivate()
		{
			if (_isActive == false)
			{
				return;
			}

			_trainSpawnController.CurrentTrainChanged -= OnCurrentTrainChanged;
			if (_runtimeModel != null)
			{
				_runtimeModel.TrainTransitStateChanged -= OnTrainTransitStateChanged;
			}

			_isActive = false;
			_currentTrain = null;
			StopAndReleaseMovementLoop();
		}

		private void OnCurrentTrainChanged(TrainView currentTrain)
		{
			_currentTrain = currentTrain;
			StopAndReleaseMovementLoop();
			RefreshMovementLoop();
		}

		private void OnTrainTransitStateChanged(TrainTransitState _)
		{
			RefreshMovementLoop();
		}

		private void RefreshMovementLoop()
		{
			if (_isActive == false)
			{
				return;
			}

			if (_currentTrain == null || _runtimeModel == null || _runtimeModel.IsTrainInTransit == false)
			{
				StopAndReleaseMovementLoop();
				return;
			}

			EnsureMovementLoopStarted();
		}

		private void EnsureMovementLoopStarted()
		{
			try
			{
				if (_movementLoopInstance == null)
				{
					_movementLoopInstance = FmodRuntime.CreateAttachedLoop(
						ProjectAudioEventPaths.TrainSounds,
						_currentTrain.transform);
					_isMovementLoopPlaying = false;
				}

				if (_isMovementLoopPlaying)
				{
					return;
				}

				FmodRuntime.Start(_movementLoopInstance);
				_isMovementLoopPlaying = true;
			}
			catch (Exception exception)
			{
				_loggerService?.LogError($"[TrainAudio] Failed to start train loop: {exception.Message}");
				StopAndReleaseMovementLoop();
			}
		}

		private void StopAndReleaseMovementLoop()
		{
			if (_movementLoopInstance == null)
			{
				_isMovementLoopPlaying = false;
				return;
			}

			try
			{
				FmodRuntime.StopAndRelease(_movementLoopInstance);
			}
			catch (Exception exception)
			{
				_loggerService?.LogError($"[TrainAudio] Failed to stop train loop: {exception.Message}");
			}
			finally
			{
				_movementLoopInstance = null;
				_isMovementLoopPlaying = false;
			}
		}

		private static class FmodRuntime
		{
			private static readonly MethodInfo CreateInstanceMethod;
			private static readonly MethodInfo AttachInstanceMethod;
			private static readonly MethodInfo StartMethod;
			private static readonly MethodInfo StopMethod;
			private static readonly MethodInfo ReleaseMethod;
			private static readonly object AllowFadeoutValue;

			static FmodRuntime()
			{
				var runtimeManagerType = FindType(RuntimeManagerTypeName);
				var eventInstanceType = FindType(EventInstanceTypeName);
				var stopModeType = FindType(StopModeTypeName);

				CreateInstanceMethod = runtimeManagerType?.GetMethod(
					"CreateInstance",
					BindingFlags.Public | BindingFlags.Static,
					null,
					new[] { typeof(string) },
					null);
				AttachInstanceMethod = runtimeManagerType?.GetMethod(
					"AttachInstanceToGameObject",
					BindingFlags.Public | BindingFlags.Static,
					null,
					new[] { eventInstanceType, typeof(Transform), typeof(bool) },
					null);
				StartMethod = eventInstanceType?.GetMethod("start", BindingFlags.Public | BindingFlags.Instance);
				StopMethod = eventInstanceType?.GetMethod(
					"stop",
					BindingFlags.Public | BindingFlags.Instance,
					null,
					new[] { stopModeType },
					null);
				ReleaseMethod = eventInstanceType?.GetMethod("release", BindingFlags.Public | BindingFlags.Instance);
				AllowFadeoutValue = stopModeType != null
					? Enum.Parse(stopModeType, "ALLOWFADEOUT")
					: null;
			}

			public static object CreateAttachedLoop(string eventPath, Transform target)
			{
				if (CreateInstanceMethod == null || AttachInstanceMethod == null || target == null)
				{
					throw new InvalidOperationException("FMOD runtime is unavailable.");
				}

				var instance = CreateInstanceMethod.Invoke(null, new object[] { eventPath });
				AttachInstanceMethod.Invoke(null, new[] { instance, target, false });
				return instance;
			}

			public static void Start(object instance)
			{
				StartMethod?.Invoke(instance, Array.Empty<object>());
			}

			public static void StopAndRelease(object instance)
			{
				StopMethod?.Invoke(instance, new[] { AllowFadeoutValue });
				ReleaseMethod?.Invoke(instance, Array.Empty<object>());
			}

			private static Type FindType(string fullName)
			{
				var assemblies = AppDomain.CurrentDomain.GetAssemblies();
				for (var i = 0; i < assemblies.Length; i++)
				{
					var type = assemblies[i].GetType(fullName, false);
					if (type != null)
					{
						return type;
					}
				}

				return null;
			}
		}
	}
}
