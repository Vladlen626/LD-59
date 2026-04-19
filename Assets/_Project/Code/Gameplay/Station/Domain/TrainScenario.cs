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
			bool hasHiddenContraband,
			bool hasIllegalMarker,
			int rewardOnCorrectDecision)
		{
			ScenarioId = Normalize(scenarioId);
			Manifest = manifest ?? new ManifestData(Array.Empty<CargoQuantity>(), string.Empty);
			ActualCargo = new CargoLoadout(actualCargo);
			HasHiddenContraband = hasHiddenContraband;
			HasIllegalMarker = hasIllegalMarker;
			RewardOnCorrectDecision = Math.Max(0, rewardOnCorrectDecision);
		}

		public string ScenarioId { get; }
		public ManifestData Manifest { get; }
		public CargoLoadout ActualCargo { get; }
		public string ActualCargoId => ActualCargo.SingleCargoId;
		public bool HasHiddenContraband { get; }
		public bool HasIllegalMarker { get; }
		public int RewardOnCorrectDecision { get; }

		private static string Normalize(string value)
		{
			return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
		}
	}
}
