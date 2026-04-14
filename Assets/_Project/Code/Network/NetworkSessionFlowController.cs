using System.Collections.Generic;
using System.Reflection;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using PlatformCore.Core;
using PlatformCore.Core.Lifecycle;
using PlatformCore.Services;
using Project.Infrastructure;
using UnityScene = UnityEngine.SceneManagement.Scene;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Project.Network
{
	public sealed class NetworkSessionFlowController : IBaseController, IActivatable, IDeactivatable
	{
		private readonly NetworkManager _networkManager;
		private readonly GameLaunchSettings _launchSettings;
		private readonly ILoggerService _loggerService;
		private bool _subscribed;

		public NetworkSessionFlowController(
			NetworkManager networkManager,
			GameLaunchSettings launchSettings,
			ILoggerService loggerService)
		{
			_networkManager = networkManager;
			_launchSettings = launchSettings;
			_loggerService = loggerService;
		}

		public void Activate()
		{
			ConfigureTransport();
			SubscribeRuntimeCallbacks();

			switch (_launchSettings.Mode)
			{
				case GameLaunchMode.MultiplayerHost:
					StartServer();
					StartClient();
					break;

				case GameLaunchMode.MultiplayerClient:
					StartClient();
					break;

				default:
					_loggerService?.LogWarning($"[Multiplayer] Unsupported launch mode: {_launchSettings.Mode}.");
					break;
			}
		}

		public void Deactivate()
		{
			StopClient();
			StopServer();
			UnsubscribeRuntimeCallbacks();
		}

		private void ConfigureTransport()
		{
			var transport = _networkManager.TransportManager?.Transport;
			if (transport == null)
			{
				return;
			}

			SetWritableProperty(transport, "Port", _launchSettings.Port);
			SetWritableProperty(transport, "ClientAddress", _launchSettings.Address);
		}

		private void StartServer()
		{
			if (GetStarted(_networkManager.ServerManager))
			{
				return;
			}

			if (!InvokeConnectionMethod(_networkManager.ServerManager, "StartConnection"))
			{
				_loggerService?.LogError("[Multiplayer] Failed to start server connection.");
				return;
			}

			_loggerService?.Log("[Multiplayer] Server start requested.");
		}

		private void StartClient()
		{
			if (GetStarted(_networkManager.ClientManager))
			{
				return;
			}

			if (!InvokeConnectionMethod(_networkManager.ClientManager, "StartConnection"))
			{
				_loggerService?.LogError("[Multiplayer] Failed to start client connection.");
				return;
			}

			_loggerService?.Log(
				$"[Multiplayer] Client start requested. Target={_launchSettings.Address}:{_launchSettings.Port}.");
		}

		private void StopServer()
		{
			if (!GetStarted(_networkManager.ServerManager))
			{
				return;
			}

			if (!InvokeStopMethod(_networkManager.ServerManager))
			{
				_loggerService?.LogWarning("[Multiplayer] Failed to stop server connection.");
				return;
			}

			_loggerService?.Log("[Multiplayer] Server stopped.");
		}

		private void StopClient()
		{
			if (!GetStarted(_networkManager.ClientManager))
			{
				return;
			}

			if (!InvokeStopMethod(_networkManager.ClientManager))
			{
				_loggerService?.LogWarning("[Multiplayer] Failed to stop client connection.");
				return;
			}

			_loggerService?.Log("[Multiplayer] Client stopped.");
		}

		private static bool GetStarted(object target)
		{
			if (target == null)
			{
				return false;
			}

			var property = target.GetType().GetProperty("Started", BindingFlags.Public | BindingFlags.Instance);
			if (property == null || property.PropertyType != typeof(bool))
			{
				return false;
			}

			if (property.GetValue(target) is bool started)
			{
				return started;
			}

			return false;
		}

		private static bool InvokeStopMethod(object target)
		{
			if (target == null)
			{
				return false;
			}

			var type = target.GetType();
			var parameterlessMethod = type.GetMethod("StopConnection", BindingFlags.Public | BindingFlags.Instance, null, System.Type.EmptyTypes, null);
			if (parameterlessMethod != null)
			{
				parameterlessMethod.Invoke(target, null);
				return true;
			}

			var boolMethod = type.GetMethod("StopConnection", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(bool) }, null);
			if (boolMethod != null)
			{
				boolMethod.Invoke(target, new object[] { true });
				return true;
			}

			return false;
		}

		private static bool InvokeConnectionMethod(object target, string methodName)
		{
			if (target == null)
			{
				return false;
			}

			var method = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, null, System.Type.EmptyTypes, null);
			if (method == null)
			{
				return false;
			}

			var result = method.Invoke(target, null);
			if (method.ReturnType == typeof(bool))
			{
				return result is bool started && started;
			}

			return true;
		}

		private static bool InvokeSingleArgumentMethod(object target, string methodName, object argument)
		{
			if (target == null || argument == null)
			{
				return false;
			}

			var type = target.GetType();
			var argumentType = argument.GetType();
			var method = type.GetMethod(
				methodName,
				BindingFlags.Public | BindingFlags.Instance,
				null,
				new[] { argumentType },
				null);

			if (method == null)
			{
				var candidates = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
				foreach (var candidate in candidates)
				{
					if (!string.Equals(candidate.Name, methodName, System.StringComparison.Ordinal))
					{
						continue;
					}

					var parameters = candidate.GetParameters();
					if (parameters.Length != 1)
					{
						continue;
					}
					if (!parameters[0].ParameterType.IsAssignableFrom(argumentType))
					{
						continue;
					}

					method = candidate;
					break;
				}
			}

			if (method == null)
			{
				return false;
			}

			method.Invoke(target, new[] { argument });
			return true;
		}

		private static void SetWritableProperty(object target, string propertyName, object value)
		{
			if (target == null || value == null)
			{
				return;
			}

			var property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
			if (property == null || !property.CanWrite)
			{
				return;
			}

			var valueType = value.GetType();
			if (!property.PropertyType.IsAssignableFrom(valueType))
			{
				if (property.PropertyType == typeof(int) && valueType == typeof(ushort))
				{
					property.SetValue(target, (int)(ushort)value);
				}

				return;
			}

			property.SetValue(target, value);
		}

		private void SubscribeRuntimeCallbacks()
		{
			if (_subscribed)
			{
				return;
			}

			_networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
			_networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
			_networkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
			_networkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;
			UnitySceneManager.sceneLoaded += OnSceneLoaded;
			_subscribed = true;
		}

		private void UnsubscribeRuntimeCallbacks()
		{
			if (!_subscribed)
			{
				return;
			}

			_networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
			_networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
			_networkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
			_networkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;
			UnitySceneManager.sceneLoaded -= OnSceneLoaded;
			_subscribed = false;
		}

		private void OnClientConnectionState(ClientConnectionStateArgs args)
		{
			_loggerService?.Log(
				$"[Multiplayer] Client connection state changed: {args.ConnectionState} (transportIndex={args.TransportIndex}).");
		}

		private void OnServerConnectionState(ServerConnectionStateArgs args)
		{
			_loggerService?.Log(
				$"[Multiplayer] Server connection state changed: {args.ConnectionState} (transportIndex={args.TransportIndex}).");

			if (args.ConnectionState == LocalConnectionState.Started)
			{
				AddAllConnectionsToLoadedScenes("ServerStarted");
			}
		}

		private void OnRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
		{
			var connectionId = connection?.ClientId ?? -1;
			_loggerService?.Log(
				$"[Multiplayer] Remote connection state changed: conn={connectionId}, state={args.ConnectionState}, transportIndex={args.TransportIndex}.");
		}

		private void OnClientLoadedStartScenes(NetworkConnection connection, bool asServer)
		{
			if (!asServer || connection == null)
			{
				return;
			}

			AddConnectionToLoadedScenes(connection, "ClientLoadedStartScenes");
			RebuildObserversForConnection(connection, "ClientLoadedStartScenes");
		}

		private void OnSceneLoaded(UnityScene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
		{
			if (!_networkManager.ServerManager.Started || !IsShareableScene(scene))
			{
				return;
			}

			AddAllConnectionsToScene(scene, $"SceneLoaded:{scene.name}:{mode}");
		}

		private void AddAllConnectionsToLoadedScenes(string reason)
		{
			if (!_networkManager.ServerManager.Started)
			{
				return;
			}

			foreach (var pair in _networkManager.ServerManager.Clients)
			{
				var connection = pair.Value;
				AddConnectionToLoadedScenes(connection, reason);
			}
		}

		private void AddConnectionToLoadedScenes(NetworkConnection connection, string reason)
		{
			if (!CanSynchronizeConnection(connection))
			{
				return;
			}

			var addedScenes = new List<string>();
			var sceneCount = UnitySceneManager.sceneCount;
			for (var i = 0; i < sceneCount; i++)
			{
				var scene = UnitySceneManager.GetSceneAt(i);
				if (!IsShareableScene(scene))
				{
					continue;
				}

				if (TryAddConnectionToScene(connection, scene))
				{
					addedScenes.Add(scene.name);
				}
			}

			if (addedScenes.Count > 0)
			{
				_loggerService?.Log(
					$"[Multiplayer] Connection {connection.ClientId} added to scenes ({reason}): {string.Join(", ", addedScenes)}.");
			}
		}

		private void AddAllConnectionsToScene(UnityScene scene, string reason)
		{
			if (!IsShareableScene(scene) || !_networkManager.ServerManager.Started)
			{
				return;
			}

			var addedCount = 0;
			foreach (var pair in _networkManager.ServerManager.Clients)
			{
				var connection = pair.Value;
				if (!CanSynchronizeConnection(connection))
				{
					continue;
				}

				if (TryAddConnectionToScene(connection, scene))
				{
					addedCount++;
				}
			}

			if (addedCount > 0)
			{
				_loggerService?.Log(
					$"[Multiplayer] Added {addedCount} connection(s) to scene '{scene.name}' ({reason}).");
			}
		}

		private void RebuildObserversForConnection(NetworkConnection connection, string reason)
		{
			if (!CanSynchronizeConnection(connection))
			{
				return;
			}

			if (!InvokeSingleArgumentMethod(_networkManager.ServerManager.Objects, "RebuildObservers", connection))
			{
				_loggerService?.LogWarning(
					$"[Multiplayer] Failed to rebuild observers for connection {connection.ClientId} ({reason}).");
				return;
			}

			_loggerService?.Log(
				$"[Multiplayer] Rebuilt observers for connection {connection.ClientId} ({reason}).");
		}

		private static bool IsShareableScene(UnityScene scene)
		{
			return scene.IsValid() && scene.isLoaded && !string.IsNullOrEmpty(scene.path);
		}

		private bool CanSynchronizeConnection(NetworkConnection connection)
		{
			return _networkManager.ServerManager.Started &&
				connection != null &&
				connection.IsActive &&
				connection.LoadedStartScenes(true);
		}

		private bool TryAddConnectionToScene(NetworkConnection connection, UnityScene scene)
		{
			var wasInScene = connection.Scenes.Contains(scene);
			_networkManager.SceneManager.AddConnectionToScene(connection, scene);
			return !wasInScene && connection.Scenes.Contains(scene);
		}
	}
}
