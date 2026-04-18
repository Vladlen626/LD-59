using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Infrastructure;
using PlatformCore.Services;
using PlatformCore.Services.Audio;
using PlatformCore.Services.Factory;
using PlatformCore.Services.Input;
using PlatformCore.Services.UI;
using Project.Gameplay.Player;
using Project.Gameplay.Scene;
using Project.Gameplay.Station.Controllers;
using Project.Gameplay.Station.Domain;
using Project.Gameplay.Station.Runtime;
using Project.Gameplay.Train;
using Project.Infrastructure;
using UnityEngine;

namespace Project.Bootstrap
{
	public sealed class GameRoot : BaseGameRoot
	{
		protected override void RegisterServices(PersistentSceneContext persistentSceneContext)
		{
			Startup.RegisterServices(_serviceLocator, persistentSceneContext);
		}

		protected override async UniTask LaunchGameAsync(PersistentSceneContext persistentSceneContext)
		{
			var launchSettings = GameLaunchSettingsResolver.Resolve();
			var sceneService = _serviceLocator.Get<ISceneService>();
			var uiService = _serviceLocator.Get<IUIService>();
			var audioService = _serviceLocator.Get<IAudioService>();
			var cursorService = _serviceLocator.Get<ICursorService>();
			var inputService = _serviceLocator.Get<IInputService>();
			var loggerService = _serviceLocator.Get<ILoggerService>();
			var objectFactory = _serviceLocator.Get<IObjectFactory>();
			var cameraService = _serviceLocator.Get<ICameraService>();

			var baseControllers = new IBaseController[]
			{
				new UISettingsController(uiService, audioService, cursorService, inputService),
			};

			await _lifecycle.RegisterControllersGroupAsync(baseControllers);

			if (sceneService.IsSceneLoaded(SceneNames.SampleScene))
			{
				sceneService.TrySetActiveScene(SceneNames.SampleScene);
			}
			else
			{
				await sceneService.LoadAndSetActiveSceneAsync(SceneNames.SampleScene, ApplicationCancellationToken);
			}

			if (launchSettings.IsMultiplayer)
			{
				LaunchMultiplayer(launchSettings, loggerService);
			}
			else
			{
				await LaunchSingleplayerAsync(
					objectFactory,
					cameraService,
					uiService,
					cursorService,
					inputService,
					loggerService);
			}
		}

		private void LaunchMultiplayer(GameLaunchSettings launchSettings, ILoggerService loggerService)
		{
			var started = MultiplayerRuntimeBridge.TryStart(
				_serviceLocator,
				_lifecycle,
				launchSettings,
				loggerService);
			if (!started)
			{
				loggerService?.LogError("[GameRoot] Multiplayer launch requested but runtime did not start.");
			}
		}

		private async UniTask LaunchSingleplayerAsync(
			IObjectFactory objectFactory,
			ICameraService cameraService,
			IUIService uiService,
			ICursorService cursorService,
			IInputService inputService,
			ILoggerService loggerService)
		{
			var playerView = await SamplePlayerFactory.SpawnAsync(objectFactory);
			if (!playerView)
			{
				loggerService?.LogError("[GameRoot] Failed to spawn local player in singleplayer mode.");
				return;
			}

			if (playerView.CameraRoot)
			{
				cameraService.AttachPrimaryCameraTo(playerView.CameraRoot);
			}

			cursorService.LockCursor();
			var runtimeControllers = new List<IBaseController>
			{
				new SamplePlayerMovementController(playerView, inputService, cursorService),
			};

			var sceneContext = Object.FindObjectOfType<SceneContext>();
			if (!sceneContext)
			{
				loggerService?.LogWarning(
					"[GameRoot] SceneContext is missing in loaded scenes. Station gameplay flow is disabled.");
			}
			else
			{
				var stationSetup = sceneContext.BuildRuntimeSetup();
				if (stationSetup.Scenarios.Count == 0)
				{
					loggerService?.LogWarning(
						"[GameRoot] Station day scenarios list is empty. Flow controller will finish day immediately.");
				}

				if (stationSetup.DayRuleSet == null)
				{
					loggerService?.LogError("[GameRoot] SceneContext returned null DayRuleSet. Station gameplay flow is disabled.");
					await _lifecycle.RegisterControllersGroupAsync(runtimeControllers.ToArray());
					return;
				}

				var stationRuntimeContext = new StationSharedRuntimeContext(
					new StationDayModel(stationSetup.DayRuleSet),
					new DebtModel(stationSetup.InitialDebt),
					new InspectionJournalModel());

				var trainSpawnController = new TrainSpawnController(
					objectFactory,
					loggerService,
					ProjectGameplayResourcePaths.Trains_CargoWagon,
					stationSetup.TrainSpawnPoint,
					stationSetup.TrainSpawnParent);

				var stationFlowController = new StationFlowController(
					stationRuntimeContext,
					new InspectionJudge(),
					trainSpawnController,
					stationSetup.Scenarios,
					loggerService);
				var daySummaryController = new DaySummaryController(
					uiService,
					cursorService,
					stationFlowController,
					stationRuntimeContext,
					loggerService);
				var stationInteractionController = new StationInteractionController(
					sceneContext,
					trainSpawnController,
					stationFlowController,
					inputService,
					cursorService,
					loggerService);

				runtimeControllers.Add(trainSpawnController);
				runtimeControllers.Add(daySummaryController);
				runtimeControllers.Add(stationFlowController);
				runtimeControllers.Add(stationInteractionController);
			}

			await _lifecycle.RegisterControllersGroupAsync(runtimeControllers.ToArray());
		}
	}
}
