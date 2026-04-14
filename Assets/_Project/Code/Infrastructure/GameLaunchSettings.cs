namespace Project.Infrastructure
{
	public enum GameLaunchMode
	{
		Singleplayer = 0,
		MultiplayerHost = 1,
		MultiplayerClient = 2,
	}

	public readonly struct GameLaunchSettings
	{
		public const string DefaultAddress = "127.0.0.1";
		public const ushort DefaultPort = 7770;

		public GameLaunchSettings(
			GameLaunchMode mode,
			string address,
			ushort port)
		{
			Mode = mode;
			Address = string.IsNullOrWhiteSpace(address) ? DefaultAddress : address;
			Port = port;
		}

		public GameLaunchMode Mode { get; }
		public string Address { get; }
		public ushort Port { get; }
		public bool IsMultiplayer => Mode != GameLaunchMode.Singleplayer;
	}
}
