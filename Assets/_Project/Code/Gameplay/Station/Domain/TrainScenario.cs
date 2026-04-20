using System.Collections.Generic;
using System;

namespace Project.Gameplay.Station.Domain
{
	public sealed class TrainScenario
	{
		public TrainScenario(
			string scenarioId,
			ManifestData manifest,
			IEnumerable<CargoQuantity> actualCargo,
			int rewardOnCorrectDecision)
		{
			ScenarioId = Normalize(scenarioId);
			Manifest = manifest ?? new ManifestData(Array.Empty<CargoQuantity>(), string.Empty);
			ActualCargo = new CargoLoadout(actualCargo);
			RewardOnCorrectDecision = Math.Max(0, rewardOnCorrectDecision);
		}

		public string ScenarioId { get; }
		public ManifestData Manifest { get; }
		public CargoLoadout ActualCargo { get; }
		public string ActualCargoId => ActualCargo.SingleCargoId;
		public int RewardOnCorrectDecision { get; }

		private static string Normalize(string value)
		{
			return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
		}
	}
}
