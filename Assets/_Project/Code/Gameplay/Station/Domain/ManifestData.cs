using System;

namespace Project.Gameplay.Station.Domain
{
	public sealed class ManifestData
	{
		public ManifestData(string declaredCargoId, string declaredDescription)
		{
			if (string.IsNullOrWhiteSpace(declaredCargoId))
			{
				throw new ArgumentException("Declared cargo id cannot be empty.", nameof(declaredCargoId));
			}
			if (string.IsNullOrWhiteSpace(declaredDescription))
			{
				throw new ArgumentException("Declared description cannot be empty.", nameof(declaredDescription));
			}

			DeclaredCargoId = declaredCargoId.Trim();
			DeclaredDescription = declaredDescription.Trim();
		}

		public string DeclaredCargoId { get; }
		public string DeclaredDescription { get; }
	}
}
