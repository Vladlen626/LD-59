using PlatformCore.Infrastructure;
using PlatformCore.Infrastructure.AsyncAwaiter;
using PlatformCore.Infrastructure.Notifications;
using PlatformCore.Infrastructure.Scene;
using PlatformCore.Infrastructure.Settings;
using PlatformCore.Infrastructure.UI;
using PlatformCore.Services;
using PlatformCore.Services.Audio;
using PlatformCore.Services.Factory;
using PlatformCore.Services.Input;

namespace Project.Infrastructure
{
	public static class Startup
	{
		public static void RegisterServices(ServiceLocator serviceLocator, PersistentSceneContext persistentSceneContext)
		{
			var logger = new LoggerService();
			serviceLocator.Register<ILoggerService, LoggerService>(logger);

			var resourceService = new ResourceService(logger);
			serviceLocator.Register<IResourceService, ResourceService>(resourceService);

			var objectFactory = new ObjectFactory(resourceService, logger);
			serviceLocator.Register<IObjectFactory, ObjectFactory>(objectFactory);

			serviceLocator.RegisterSceneManagementFoundation(persistentSceneContext, logger);

			var cameraService = new CameraService(objectFactory);
			serviceLocator.Register<ICameraShakeService, CameraService>(cameraService);
			serviceLocator.Register<ICameraService, CameraService>(cameraService);

			var audioService = new AudioBaseService(logger);
			serviceLocator.Register<IAudioService, AudioBaseService>(audioService);

			var uiService = serviceLocator.RegisterUIFoundation(persistentSceneContext, logger, resourceService);
			serviceLocator.RegisterGlobalNotificationsFoundation(uiService, objectFactory, audioService);

			var inputService = new InputBaseService();
			serviceLocator.Register<IInputService, InputBaseService>(inputService);

			serviceLocator.RegisterAsyncAwaiterFoundation();
			serviceLocator.RegisterSettingsFoundation();
			serviceLocator.RegisterAudioSettingsApplier(audioService);
			serviceLocator.RegisterCameraSettingsApplier(cameraService);
		}
	}
}
