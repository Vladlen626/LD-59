namespace Project.Gameplay.Station.Domain
{
	public sealed class ManifestData
	{
		public ManifestData(string declaredCargoId, string declaredDescription)
		{
			DeclaredCargoId = Normalize(declaredCargoId);
			DeclaredDescription = Normalize(declaredDescription);
		}

		public string DeclaredCargoId { get; }
		public string DeclaredDescription { get; }

		private static string Normalize(string value)
		{
			return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
		}
	}
}
