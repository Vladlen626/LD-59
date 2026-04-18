using System;
using System.Collections.Generic;

namespace Project.Gameplay.Station.Domain
{
	public sealed class InspectionJudge
	{
		public InspectionResult Evaluate(
			DayRuleSet dayRuleSet,
			TrainScenario scenario,
			PlayerDecision playerDecision)
		{
			if (dayRuleSet == null)
			{
				throw new ArgumentNullException(nameof(dayRuleSet));
			}
			if (scenario == null)
			{
				throw new ArgumentNullException(nameof(scenario));
			}

			var isForbiddenCargo = dayRuleSet.IsCargoForbidden(scenario.ActualCargoId);
			var isManifestMismatch = !string.Equals(
				scenario.Manifest.DeclaredCargoId,
				scenario.ActualCargoId,
				StringComparison.OrdinalIgnoreCase);
			var hasHiddenContraband = scenario.HasHiddenContraband;
			var hasIllegalMarker = scenario.HasIllegalMarker;

			var hasContraband =
				isForbiddenCargo ||
				isManifestMismatch ||
				hasHiddenContraband ||
				hasIllegalMarker;

			var expectedDecision = hasContraband
				? PlayerDecision.SendBack
				: PlayerDecision.AllowPassage;

			if (playerDecision == expectedDecision)
			{
				return new InspectionResult(
					InspectionVerdict.Correct,
					playerDecision,
					expectedDecision,
					0,
					Array.Empty<InspectionErrorReason>());
			}

			var errorReasons = BuildErrorReasons(
				playerDecision,
				isForbiddenCargo,
				isManifestMismatch,
				hasHiddenContraband,
				hasIllegalMarker);

			var penalty = playerDecision == PlayerDecision.AllowPassage
				? dayRuleSet.MissedContrabandPenalty
				: dayRuleSet.WrongfulRejectionPenalty;

			return new InspectionResult(
				InspectionVerdict.Incorrect,
				playerDecision,
				expectedDecision,
				penalty,
				errorReasons);
		}

		private static IReadOnlyList<InspectionErrorReason> BuildErrorReasons(
			PlayerDecision playerDecision,
			bool isForbiddenCargo,
			bool isManifestMismatch,
			bool hasHiddenContraband,
			bool hasIllegalMarker)
		{
			var reasons = new List<InspectionErrorReason>();

			if (playerDecision == PlayerDecision.SendBack)
			{
				reasons.Add(InspectionErrorReason.RejectedCleanTrain);
				return reasons;
			}

			if (isForbiddenCargo)
			{
				reasons.Add(InspectionErrorReason.AllowedForbiddenCargo);
			}

			if (isManifestMismatch)
			{
				reasons.Add(InspectionErrorReason.AllowedManifestMismatch);
			}

			if (hasHiddenContraband)
			{
				reasons.Add(InspectionErrorReason.AllowedHiddenContraband);
			}

			if (hasIllegalMarker)
			{
				reasons.Add(InspectionErrorReason.AllowedIllegalMarker);
			}

			return reasons;
		}
	}
}
