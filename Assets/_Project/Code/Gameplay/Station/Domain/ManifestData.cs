using System.Collections.Generic;

namespace Project.Gameplay.Station.Domain
{
	public sealed class ManifestData
	{
		public ManifestData(
			IEnumerable<CargoQuantity> declaredCargo,
			string declaredDescription)
		{
			DeclaredCargo = new CargoLoadout(declaredCargo);
			DeclaredDescription = Normalize(declaredDescription);
		}

		public CargoLoadout DeclaredCargo { get; }
		public string DeclaredCargoId => DeclaredCargo.SingleCargoId;
		public string DeclaredDescription { get; }

		private static string Normalize(string value)
		{
			return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
		}
	}
}
