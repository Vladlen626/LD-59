using Cysharp.Threading.Tasks;
using PlatformCore.Core;
using PlatformCore.Infrastructure;
using PlatformCore.Services;
using PlatformCore.Services.Audio;
using PlatformCore.Services.Factory;
using PlatformCore.Services.Input;
using PlatformCore.Services.UI;
using Project.Gameplay.Player;
using Project.Infrastructure;

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
				await LaunchSingleplayerAsync(objectFactory, cameraService, cursorService, inputService, loggerService);
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

			var playerControllers = new IBaseController[]
			{
				new SamplePlayerMovementController(playerView, inputService, cursorService),
			};

			await _lifecycle.RegisterControllersGroupAsync(playerControllers);
		}
	}
}
