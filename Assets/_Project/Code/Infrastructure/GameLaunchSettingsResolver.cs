using System;
namespace Project.Infrastructure
{
	public static class GameLaunchSettingsResolver
	{
		private const string HostArg = "-host";
		private const string ClientArg = "-client";
		private const string AddressArg = "-address";
		private const string PortArg = "-port";

		public static GameLaunchSettings Resolve()
		{
			var args = Environment.GetCommandLineArgs();
			if (args == null || args.Length == 0)
			{
				return new GameLaunchSettings(
					GameLaunchMode.Singleplayer,
					GameLaunchSettings.DefaultAddress,
					GameLaunchSettings.DefaultPort);
			}

			var mode = GameLaunchMode.Singleplayer;
			var address = GameLaunchSettings.DefaultAddress;
			var port = GameLaunchSettings.DefaultPort;
			var hostRequested = false;

			for (var i = 0; i < args.Length; i++)
			{
				var arg = args[i];
				if (string.Equals(arg, HostArg, StringComparison.OrdinalIgnoreCase))
				{
					hostRequested = true;
					mode = GameLaunchMode.MultiplayerHost;
					continue;
				}

				if (string.Equals(arg, ClientArg, StringComparison.OrdinalIgnoreCase))
				{
					if (!hostRequested)
					{
						mode = GameLaunchMode.MultiplayerClient;
					}

					continue;
				}

				if (string.Equals(arg, AddressArg, StringComparison.OrdinalIgnoreCase))
				{
					if (TryReadNextValue(args, i, out var next))
					{
						address = next;
						i++;
					}

					continue;
				}

				if (string.Equals(arg, PortArg, StringComparison.OrdinalIgnoreCase))
				{
					if (TryReadNextValue(args, i, out var next) && ushort.TryParse(next, out var parsedPort))
					{
						port = parsedPort;
						i++;
					}

					continue;
				}
			}

			return new GameLaunchSettings(mode, address, port);
		}

		private static bool TryReadNextValue(string[] args, int index, out string value)
		{
			value = string.Empty;

			var nextIndex = index + 1;
			if (nextIndex >= args.Length)
			{
				return false;
			}

			var next = args[nextIndex];
			if (string.IsNullOrWhiteSpace(next) || next.StartsWith("-", StringComparison.Ordinal))
			{
				return false;
			}

			value = next;
			return true;
		}
	}
}
