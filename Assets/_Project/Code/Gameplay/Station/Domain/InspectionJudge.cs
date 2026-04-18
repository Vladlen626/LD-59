using System;
using System.Collections.Generic;

namespace Project.Gameplay.Station.Domain
{
	public sealed class InspectionJudge
	{
		private static readonly IReadOnlyList<InspectionErrorReason> RejectedCleanTrainReason =
			new[] { InspectionErrorReason.RejectedCleanTrain };

		public InspectionResult Evaluate(
			DayRuleSet dayRuleSet,
			TrainScenario scenario,
			PlayerDecision playerDecision)
		{
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
			if (playerDecision == PlayerDecision.SendBack)
			{
				return RejectedCleanTrainReason;
			}

			var reasons = new List<InspectionErrorReason>(4);

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
