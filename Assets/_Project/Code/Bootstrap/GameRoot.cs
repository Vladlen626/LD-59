using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Infrastructure;
using PlatformCore.Services;
using PlatformCore.Services.Audio;
using PlatformCore.Services.Factory;
using PlatformCore.Services.Input;
using PlatformCore.Services.Notifications;
using PlatformCore.Services.UI;
using Project.Gameplay.Player;
using Project.Gameplay.Scene;
using Project.Gameplay.Station.Configs;
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
			var globalNotificationService = _serviceLocator.Get<IGlobalNotificationService>();
			var loggerService = _serviceLocator.Get<ILoggerService>();
			var objectFactory = _serviceLocator.Get<IObjectFactory>();
			var cameraService = _serviceLocator.Get<ICameraService>();
			var configService = _serviceLocator.Get<ConfigService>();

			await StartBackgroundAudioAsync(audioService);

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

			cameraService.SetActiveCamera(CameraIds.MainMenu, CameraTransitions.Instant);

			var gameStartModel = new GameStartModel();
			await _lifecycle.RegisterControllersGroupAsync(
				new IBaseController[]
				{
					new UIMainMenuController(uiService, cursorService, gameStartModel),
				});
			await gameStartModel.WaitUntilStartedAsync();

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
					globalNotificationService,
					configService,
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
			IGlobalNotificationService globalNotificationService,
			ConfigService configService,
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
			cameraService.SetActiveCamera(CameraIds.Primary, CameraTransitions.Instant);

			cursorService.LockCursor();
			var playerMovementController = new SamplePlayerMovementController(playerView, inputService, cursorService);
			IBaseController[] controllers = new IBaseController[]
			{
				playerMovementController,
			};

			var sceneContext = Object.FindAnyObjectByType<SceneContext>();
			if (!sceneContext)
			{
				loggerService?.LogWarning(
					"[GameRoot] SceneContext is missing in loaded scenes. Station gameplay flow is disabled.");
			}
			else
			{
				var stationSetup = sceneContext.BuildRuntimeSetup();
				var stationCampaignConfig =
					await configService.GetFirstOrDefaultAsync<StationCampaignConfig>(StationCampaignConfigResourcePaths.Campaign);
				var dayContents = await LoadStationDayContentsAsync(configService, stationCampaignConfig, loggerService);
				if (dayContents.Count == 0)
				{
					loggerService?.LogError("[GameRoot] Station campaign/day JSON config is missing. Station gameplay flow is disabled.");
				}
				else
				{
					var campaignProgress = new StationCampaignProgressModel(
						dayContents,
						stationCampaignConfig?.max_cases_per_day ?? StationDayConfig.MaxTrainScenariosPerDay,
						stationCampaignConfig?.allow_endless_after_campaign ?? false,
						stationCampaignConfig?.endless_min_cases ?? 6,
						stationCampaignConfig?.endless_max_cases ?? StationDayConfig.MaxTrainScenariosPerDay);
					var initialDay = campaignProgress.CurrentDay;
					if (initialDay == null)
					{
						loggerService?.LogError("[GameRoot] Campaign loaded but does not have a valid first day.");
						await _lifecycle.RegisterControllersGroupAsync(controllers);
						return;
					}

					if (initialDay.Scenarios.Count == 0)
					{
						loggerService?.LogWarning(
							"[GameRoot] First authored station day has no scenarios. Flow controller will finish day immediately.");
					}

					var stationRuntimeContext = new StationSharedRuntimeContext(
						new StationDayModel(initialDay.RuleSet),
						new DebtModel(initialDay.InitialDebt),
						new InspectionJournalModel());

					var trainSpawnController = new TrainSpawnController(
						objectFactory,
						loggerService,
						ProjectGameplayResourcePaths.Trains_CargoWagon,
						stationSetup.TrainSpawnPoint,
						stationSetup.TrainSpawnParent);
					var cargoCatalogLoader = new CargoCatalogLoader(configService);
					var trainCargoPresentationController = new TrainCargoPresentationController(
						objectFactory,
						trainSpawnController,
						cargoCatalogLoader,
						loggerService);
					var trainMovementController = new TrainMovementController(
						trainSpawnController,
						stationRuntimeContext,
						stationSetup,
						cameraService,
						loggerService);
					var stationFlowController = new StationFlowController(
						stationRuntimeContext,
						new InspectionJudge(),
						campaignProgress,
						trainSpawnController,
						trainCargoPresentationController,
						trainMovementController,
						globalNotificationService,
						loggerService);
					var stationInspectionInfoController = new StationInspectionInfoController(
						sceneContext,
						stationRuntimeContext,
						cargoCatalogLoader,
						loggerService);
					var daySummaryController = new DaySummaryController(
						uiService,
						cursorService,
						stationFlowController,
						stationRuntimeContext,
						loggerService);
					var stationInteractionController = new StationInteractionController(
						playerView,
						sceneContext,
						stationRuntimeContext,
						trainSpawnController,
						stationFlowController,
						inputService,
						cursorService,
						loggerService);
					var uiHintViewController = new UIHintViewController(
						uiService,
						stationInteractionController);

					controllers = new IBaseController[]
					{
						playerMovementController,
						trainSpawnController,
						trainCargoPresentationController,
						trainMovementController,
						daySummaryController,
						stationInspectionInfoController,
						stationFlowController,
						stationInteractionController,
						uiHintViewController,
					};
				}
			}
			await _lifecycle.RegisterControllersGroupAsync(controllers);
		}

		private static async UniTask StartBackgroundAudioAsync(IAudioService audioService)
		{
			if (audioService == null)
			{
				return;
			}

			await UniTask.WhenAll(
				audioService.PrewarmEventAsync(SoundNames.DesertAmbience),
				audioService.PrewarmEventAsync(SoundNames.Ost_1));

			audioService.PlaySoundParallel(SoundNames.DesertAmbience);
			audioService.PlaySoundParallel(SoundNames.Ost_1);
		}

		private async UniTask<List<StationDayContent>> LoadStationDayContentsAsync(
			ConfigService configService,
			StationCampaignConfig campaignConfig,
			ILoggerService loggerService)
		{
			var maxCasesPerDay = campaignConfig?.max_cases_per_day ?? StationDayConfig.MaxTrainScenariosPerDay;
			var dayContents = new List<StationDayContent>();

			if (campaignConfig != null && campaignConfig.day_resource_paths.Count > 0)
			{
				for (var i = 0; i < campaignConfig.day_resource_paths.Count; i++)
				{
					var resourcePath = campaignConfig.day_resource_paths[i];
					var dayContent = await LoadStationDayContentAsync(configService, resourcePath, maxCasesPerDay, loggerService);
					if (dayContent == null)
					{
						continue;
					}

					dayContents.Add(dayContent);
				}

				if (dayContents.Count > 0)
				{
					return dayContents;
				}

				loggerService?.LogWarning(
					"[GameRoot] Station campaign config was loaded but no valid day resources were found. Falling back to single day mode.");
			}

			var fallbackDayContent = await LoadStationDayContentAsync(
				configService,
				StationDayConfigResourcePaths.Day,
				StationDayConfig.MaxTrainScenariosPerDay,
				loggerService);
			if (fallbackDayContent != null)
			{
				dayContents.Add(fallbackDayContent);
			}

			return dayContents;
		}

		private async UniTask<StationDayContent> LoadStationDayContentAsync(
			ConfigService configService,
			string resourcePath,
			int maxCasesPerDay,
			ILoggerService loggerService)
		{
			if (string.IsNullOrWhiteSpace(resourcePath))
			{
				return null;
			}

			var loadedConfigs = await configService.GetConfigsAsync<StationDayConfig>(resourcePath);
			StationDayConfig stationDayConfig = null;
			foreach (var pair in loadedConfigs)
			{
				stationDayConfig = pair.Value;
				break;
			}

			if (stationDayConfig == null)
			{
				loggerService?.LogWarning($"[GameRoot] Station day config at '{resourcePath}' could not be loaded.");
				return null;
			}

			stationDayConfig.BuildRuntimeData(out var dayRuleSet, out var dayScenarios);
			var cappedScenarios = CapDayScenarios(dayScenarios, maxCasesPerDay);
			return new StationDayContent(resourcePath, stationDayConfig.initial_debt, dayRuleSet, cappedScenarios);
		}

		private static List<TrainScenario> CapDayScenarios(List<TrainScenario> scenarios, int maxCasesPerDay)
		{
			if (scenarios == null || scenarios.Count == 0)
			{
				return new List<TrainScenario>();
			}

			var safeMaxCases = Mathf.Max(1, maxCasesPerDay);
			if (scenarios.Count <= safeMaxCases)
			{
				return scenarios;
			}

			return scenarios.GetRange(0, safeMaxCases);
		}
	}
}
