using System;

namespace Project.Gameplay.Station.Runtime
{
	public sealed class DayLivingCostData
	{
		public DayLivingCostData(string label, int amount)
		{
			Label = Normalize(label);
			Amount = Math.Max(0, amount);
		}

		public string Label { get; }
		public int Amount { get; }
		public bool HasCost => Amount > 0;

		private static string Normalize(string value)
		{
			return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
		}
	}
}
