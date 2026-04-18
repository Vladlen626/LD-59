using System;

namespace Project.Gameplay.Station.Domain
{
	public sealed class TrainScenario
	{
		public TrainScenario(
			string scenarioId,
			ManifestData manifest,
			string actualCargoId,
			bool hasHiddenContraband,
			bool hasIllegalMarker,
			int rewardOnCorrectDecision)
		{
			if (string.IsNullOrWhiteSpace(scenarioId))
			{
				throw new ArgumentException("Scenario id cannot be empty.", nameof(scenarioId));
			}
			if (manifest == null)
			{
				throw new ArgumentNullException(nameof(manifest));
			}
			if (string.IsNullOrWhiteSpace(actualCargoId))
			{
				throw new ArgumentException("Actual cargo id cannot be empty.", nameof(actualCargoId));
			}
			if (rewardOnCorrectDecision < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(rewardOnCorrectDecision), "Reward cannot be negative.");
			}

			ScenarioId = scenarioId.Trim();
			Manifest = manifest;
			ActualCargoId = actualCargoId.Trim();
			HasHiddenContraband = hasHiddenContraband;
			HasIllegalMarker = hasIllegalMarker;
			RewardOnCorrectDecision = rewardOnCorrectDecision;
		}

		public string ScenarioId { get; }
		public ManifestData Manifest { get; }
		public string ActualCargoId { get; }
		public bool HasHiddenContraband { get; }
		public bool HasIllegalMarker { get; }
		public int RewardOnCorrectDecision { get; }
	}
}
