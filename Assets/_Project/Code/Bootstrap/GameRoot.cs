using System;
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
using PlatformCore.Services.UI.SplashScreen;
using Project.Gameplay.Player;
using Project.Gameplay.Scene;
using Project.Gameplay.Station.Configs;
using Project.Gameplay.Station.Controllers;
using Project.Gameplay.Station.Domain;
using Project.Gameplay.Station.Runtime;
using Project.Gameplay.Train;
using Project.Infrastructure;
using UnityEngine;
using Object = UnityEngine.Object;

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
			var splashScreenService = _serviceLocator.Get<ISplashScreenService>();
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

			if (sceneService.IsSceneLoaded(SceneNames.SampleScene) == false)
			{
				await sceneService.LoadAndSetActiveSceneAsync(SceneNames.SampleScene, ApplicationCancellationToken);
			}
			sceneService.TrySetActiveScene(SceneNames.SampleScene);
			var menuSceneContext = Object.FindAnyObjectByType<SceneContext>();

			cameraService.SetActiveCamera(CameraIds.MainMenu, CameraTransitions.Instant);

			var gameStartModel = new GameStartModel();
			MainMenuStartInteractableView startButton = null;
			Action onStartClicked = null;
			if (menuSceneContext && menuSceneContext.MainMenuStartButton)
			{
				startButton = menuSceneContext.MainMenuStartButton;
				onStartClicked = () =>
				{
					menuSceneContext.MainMenuStartVisualRoot?.Hide();
					gameStartModel.Start();
				};
				startButton.Clicked += onStartClicked;
			}

			if (cursorService.IsCursorLocked)
			{
				cursorService.UnlockCursor();
			}

			await gameStartModel.WaitUntilStartedAsync();
			if (startButton && onStartClicked != null)
			{
				startButton.Clicked -= onStartClicked;
			}
			await splashScreenService.FadeInAsync(ProjectSplashScreenSettings.MainMenuToGameFadeInSeconds);

			if (launchSettings.IsMultiplayer)
			{
				LaunchMultiplayer(launchSettings, loggerService);
				await splashScreenService.FadeOutAsync(ProjectSplashScreenSettings.MainMenuToGameFallbackFadeOutSeconds);
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
					splashScreenService,
					audioService,
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
			ISplashScreenService splashScreenService,
			IAudioService audioService,
			ConfigService configService,
			ILoggerService loggerService)
		{
			var playerView = await SamplePlayerFactory.SpawnAsync(objectFactory);
			if (!playerView)
			{
				loggerService?.LogError("[GameRoot] Failed to spawn local player in singleplayer mode.");
				await splashScreenService.FadeOutAsync(ProjectSplashScreenSettings.MainMenuToGameFallbackFadeOutSeconds);
				return;
			}

			if (playerView.CameraRoot)
			{
				cameraService.AttachPrimaryCameraTo(playerView.CameraRoot);
			}
			cameraService.SetActiveCamera(CameraIds.Primary, CameraTransitions.Instant);

			cursorService.LockCursor();
			var playerMovementController = new SamplePlayerMovementController(
				playerView,
				inputService,
				cursorService,
				audioService);
			CampaignEndingController campaignEndingController = null;
			UILetterController uiLetterController = null;
			UIWakeTransitionController uiWakeTransitionController = null;
			NightShiftIntroController nightShiftIntroController = null;
			var handlesSplashFadeOut = false;
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
						stationCampaignConfig?.endless_max_cases ?? StationDayConfig.MaxTrainScenariosPerDay,
						stationCampaignConfig?.endless_min_occupied_slots_per_train ?? 6,
						stationCampaignConfig?.endless_max_occupied_slots_per_train ??
							StationCampaignConfig.MaxOccupiedSlotsPerTrain);
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
						stationSetup,
						stationSetup.TrainSpawnPoint,
						stationSetup.TrainSpawnParent);
					var cargoCatalogLoader = new CargoCatalogLoader(configService);
					var endlessDayGenerator = new StationEndlessDayGenerator(cargoCatalogLoader, loggerService);
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
						audioService,
						loggerService);
					var trainAudioController = new TrainAudioController(
						trainSpawnController,
						stationRuntimeContext,
						loggerService);
					var stationFlowController = new StationFlowController(
						stationRuntimeContext,
						new InspectionJudge(),
						campaignProgress,
						trainSpawnController,
						trainCargoPresentationController,
						trainMovementController,
						endlessDayGenerator,
						globalNotificationService,
						loggerService);
					var stationInspectionInfoController = new StationInspectionInfoController(
						sceneContext,
						stationRuntimeContext,
						cargoCatalogLoader,
						loggerService);
					campaignEndingController = new CampaignEndingController(
						inputService,
						cursorService,
						loggerService,
						objectFactory);
					var stationInteractionController = new StationInteractionController(
						playerView,
						sceneContext,
						stationRuntimeContext,
						trainSpawnController,
						stationFlowController,
						inputService,
						cursorService,
						audioService,
						loggerService);
					var boundaryHomePosition = stationSetup.RoomSetup.WakePoint
						? stationSetup.RoomSetup.WakePoint.position
						: playerView.transform.position;
					var playerBoundaryController = new PlayerBoundaryController(
						playerView,
						playerMovementController,
						globalNotificationService,
						boundaryHomePosition,
						50f);
					uiLetterController = new UILetterController(
						inputService,
						cursorService,
						loggerService,
						objectFactory,
						stationInteractionController);
					var daySummaryController = new DaySummaryController(
						uiService,
						cursorService,
						stationFlowController,
						stationRuntimeContext,
						uiLetterController,
						campaignEndingController,
						splashScreenService,
						audioService,
						loggerService);
					uiWakeTransitionController = new UIWakeTransitionController(loggerService, objectFactory);
					nightShiftIntroController = new NightShiftIntroController(
						playerView,
						playerMovementController,
						stationSetup,
						stationFlowController,
						stationInteractionController,
						uiWakeTransitionController,
						uiLetterController,
						cursorService,
						cameraService,
						audioService,
						objectFactory,
						splashScreenService,
						globalNotificationService);
					var uiHintViewController = new UIHintViewController(
						uiService,
						stationInteractionController);

					controllers = new IBaseController[]
					{
						playerMovementController,
						playerBoundaryController,
						trainSpawnController,
						trainCargoPresentationController,
						trainMovementController,
						trainAudioController,
						stationInspectionInfoController,
						stationInteractionController,
						uiHintViewController,
						campaignEndingController,
						uiWakeTransitionController,
						uiLetterController,
						nightShiftIntroController,
						daySummaryController,
						stationFlowController,
					};
					handlesSplashFadeOut = true;
				}
			}
			await _lifecycle.RegisterControllersGroupAsync(controllers);
			if (handlesSplashFadeOut == false)
			{
				await splashScreenService.FadeOutAsync(ProjectSplashScreenSettings.MainMenuToGameFallbackFadeOutSeconds);
			}
		}

		private static async UniTask StartBackgroundAudioAsync(IAudioService audioService)
		{
			if (audioService == null)
			{
				return;
			}

			await UniTask.WhenAll(
				audioService.PrewarmEventAsync(SoundNames.DesertAmbience),
				audioService.PrewarmEventAsync(SoundNames.Ost_2),
				audioService.PrewarmEventAsync(SoundNames.Bell),
				audioService.PrewarmEventAsync(SoundNames.BuildingDoor),
				audioService.PrewarmEventAsync(SoundNames.DayEnd),
				audioService.PrewarmEventAsync(SoundNames.DoorKnocking),
				audioService.PrewarmEventAsync(SoundNames.JumpSample),
				audioService.PrewarmEventAsync(SoundNames.Lever),
				audioService.PrewarmEventAsync(SoundNames.Notification),
				audioService.PrewarmEventAsync(SoundNames.TrainDoorOpen),
				audioService.PrewarmEventAsync(SoundNames.TrainDoorClose),
				audioService.PrewarmEventAsync(SoundNames.TrainSounds),
				audioService.PrewarmEventAsync(SoundNames.TrainStopping),
				audioService.PrewarmEventAsync(SoundNames.WoodFootSteps));

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
			return new StationDayContent(
				resourcePath,
				stationDayConfig.initial_debt,
				dayRuleSet,
				cappedScenarios,
				stationDayConfig.BuildShiftLetterData(),
				stationDayConfig.BuildFinalNoticeData(),
				stationDayConfig.BuildLivingCostData(),
				stationDayConfig.BuildEndDayNote());
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
