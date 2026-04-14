using System.Reflection;
using Cysharp.Threading.Tasks;
using FishNet;
using FishNet.Component.Spawning;
using PlatformCore.Infrastructure;
using PlatformCore.Infrastructure.Network.FishNet;
using PlatformCore.Services;
using PlatformCore.Services.Network;
using Project.Infrastructure;

namespace Project.Network
{
	public static class MultiplayerRuntimeEntryPoint
	{
		private const string PlayerPrefabFieldName = "_playerPrefab";
		private static readonly FieldInfo PlayerPrefabFieldInfo =
			typeof(PlayerSpawner).GetField(PlayerPrefabFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
		private static PlayerSpawner _diagnosticsSpawner;
		private static ILoggerService _diagnosticsLogger;

		public static bool Start(
			ServiceLocator serviceLocator,
			LifecycleService lifecycleService,
			GameLaunchSettings launchSettings,
			ILoggerService loggerService)
		{
			if (!launchSettings.IsMultiplayer)
			{
				return false;
			}

			var networkManager = InstanceFinder.NetworkManager;
			if (!networkManager)
			{
				loggerService?.LogError(
					"[Multiplayer] FishNet NetworkManager is missing in loaded scenes. " +
					"Add NetworkManager + PlayerSpawner to Persistent scene.");
				return false;
			}
			if (!ValidateSpawnerConfiguration(networkManager, loggerService, out var playerSpawner))
			{
				return false;
			}
			TryRegisterSpawnerDiagnostics(playerSpawner, loggerService);

			if (!serviceLocator.TryGet<INetworkSessionService>(out _))
			{
				serviceLocator.RegisterFishNetFoundation(loggerService);
			}

			var sessionController = new NetworkSessionFlowController(networkManager, launchSettings, loggerService);
			lifecycleService.RegisterAsync(sessionController).Forget(exception =>
				loggerService?.LogError($"[Multiplayer] Failed to register network session controller: {exception}"));

			loggerService?.Log($"[Multiplayer] Requested mode: {launchSettings.Mode}, address={launchSettings.Address}, port={launchSettings.Port}");
			return true;
		}

		private static bool ValidateSpawnerConfiguration(
			FishNet.Managing.NetworkManager networkManager,
			ILoggerService loggerService,
			out PlayerSpawner playerSpawner)
		{
			playerSpawner = networkManager.GetComponent<PlayerSpawner>() ??
				networkManager.GetComponentInChildren<PlayerSpawner>(true);
			if (!playerSpawner)
			{
				loggerService?.LogError(
					"[Multiplayer] PlayerSpawner is missing. " +
					"Add FishNet PlayerSpawner to the NetworkManager object in Persistent scene.");
				return false;
			}

			var playerPrefab = PlayerPrefabFieldInfo?.GetValue(playerSpawner) as FishNet.Object.NetworkObject;
			if (!playerPrefab)
			{
				loggerService?.LogError(
					"[Multiplayer] PlayerSpawner.PlayerPrefab is not assigned. " +
					"Assign NetworkPlayer prefab on PlayerSpawner.");
				return false;
			}

			if (!playerPrefab.GetComponent<NetworkOwnedPlayerBootstrap>())
			{
				loggerService?.LogWarning(
					"[Multiplayer] NetworkOwnedPlayerBootstrap is missing on PlayerSpawner.PlayerPrefab. " +
					"Owner input/camera bootstrap will not run.");
			}

			return true;
		}

		private static void TryRegisterSpawnerDiagnostics(PlayerSpawner playerSpawner, ILoggerService loggerService)
		{
			if (!playerSpawner)
			{
				return;
			}

			if (_diagnosticsSpawner == playerSpawner)
			{
				_diagnosticsLogger = loggerService;
				return;
			}

			if (_diagnosticsSpawner)
			{
				_diagnosticsSpawner.OnSpawned -= OnPlayerSpawnerSpawned;
			}

			_diagnosticsSpawner = playerSpawner;
			_diagnosticsLogger = loggerService;
			_diagnosticsSpawner.OnSpawned += OnPlayerSpawnerSpawned;
		}

		private static void OnPlayerSpawnerSpawned(FishNet.Object.NetworkObject spawnedObject)
		{
			if (!spawnedObject)
			{
				return;
			}

			var ownerId = spawnedObject.Owner != null && spawnedObject.Owner.IsValid
				? spawnedObject.Owner.ClientId.ToString()
				: "none";
			_diagnosticsLogger?.Log(
				$"[Multiplayer] PlayerSpawner spawned objectId={spawnedObject.ObjectId}, owner={ownerId}, scene={spawnedObject.gameObject.scene.name}.");
		}
	}
}
