using System;

namespace Project.Gameplay.Station.Domain
{
	public sealed class CargoQuantity
	{
		public CargoQuantity(string cargoId, int quantity)
		{
			CargoId = Normalize(cargoId);
			Quantity = Math.Max(0, quantity);
		}

		public string CargoId { get; }
		public int Quantity { get; }

		public bool IsValid => string.IsNullOrWhiteSpace(CargoId) == false && Quantity > 0;

		private static string Normalize(string value)
		{
			return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
		}
	}
}
