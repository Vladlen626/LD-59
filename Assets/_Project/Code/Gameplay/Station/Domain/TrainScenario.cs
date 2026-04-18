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
			if (manifest == null)
			{
				throw new ArgumentNullException(nameof(manifest));
			}

			ScenarioId = Normalize(scenarioId);
			Manifest = manifest;
			ActualCargoId = Normalize(actualCargoId);
			HasHiddenContraband = hasHiddenContraband;
			HasIllegalMarker = hasIllegalMarker;
			RewardOnCorrectDecision = Math.Max(0, rewardOnCorrectDecision);
		}

		public string ScenarioId { get; }
		public ManifestData Manifest { get; }
		public string ActualCargoId { get; }
		public bool HasHiddenContraband { get; }
		public bool HasIllegalMarker { get; }
		public int RewardOnCorrectDecision { get; }

		private static string Normalize(string value)
		{
			return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
		}
	}
}
