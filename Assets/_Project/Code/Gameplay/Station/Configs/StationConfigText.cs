namespace Project.Gameplay.Station.Configs
{
	internal static class StationConfigText
	{
		public static string Normalize(string value)
		{
			return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
		}
	}
}
