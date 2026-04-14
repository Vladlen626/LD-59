using System;
using System.Reflection;
using PlatformCore.Infrastructure;
using PlatformCore.Services;

namespace Project.Infrastructure
{
	public static class MultiplayerRuntimeBridge
	{
		private const string FishNetManagerTypeName = "FishNet.Managing.NetworkManager, FishNet.Runtime";
		private const string EntryPointTypeName = "Project.Network.MultiplayerRuntimeEntryPoint, _Project.Network";
		private const string StartMethodName = "Start";

		public static bool TryStart(
			ServiceLocator serviceLocator,
			LifecycleService lifecycleService,
			GameLaunchSettings launchSettings,
			ILoggerService loggerService)
		{
			if (!launchSettings.IsMultiplayer)
			{
				return false;
			}

			if (Type.GetType(FishNetManagerTypeName) == null)
			{
				loggerService?.LogError(
					"[Multiplayer] FishNet runtime assembly is unavailable. " +
					"Install FishNet or reimport package assemblies.");
				return false;
			}

			var entryType = Type.GetType(EntryPointTypeName);
			if (entryType == null)
			{
				loggerService?.LogError(
					"[Multiplayer] Multiplayer mode requested, but _Project.Network entry point is unavailable. " +
					"Enable LEN_PROJECT_HAS_FISHNET and LEN_PLATFORMCORE_HAS_FISHNET define symbols, then recompile.");
				return false;
			}

			var startMethod = entryType.GetMethod(StartMethodName, BindingFlags.Public | BindingFlags.Static);
			if (startMethod == null)
			{
				loggerService?.LogError("[Multiplayer] Multiplayer entry point exists, but Start method is missing.");
				return false;
			}
			if (startMethod.ReturnType != typeof(bool))
			{
				loggerService?.LogError("[Multiplayer] Multiplayer Start method must return bool.");
				return false;
			}
			var startParameters = startMethod.GetParameters();
			if (startParameters.Length != 4)
			{
				loggerService?.LogError(
					"[Multiplayer] Multiplayer Start method has invalid signature. " +
					"Expected parameters: (ServiceLocator, LifecycleService, GameLaunchSettings, ILoggerService).");
				return false;
			}

			object result;
			try
			{
				result = startMethod.Invoke(null, new object[]
				{
					serviceLocator,
					lifecycleService,
					launchSettings,
					loggerService,
				});
			}
			catch (Exception exception)
			{
				loggerService?.LogError($"[Multiplayer] Failed to start multiplayer runtime: {exception}");
				return false;
			}

			if (result is bool started)
			{
				return started;
			}

			loggerService?.LogError("[Multiplayer] Multiplayer Start returned invalid result.");
			return false;
		}
	}
}
