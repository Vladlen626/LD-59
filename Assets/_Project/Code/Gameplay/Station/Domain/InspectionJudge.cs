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

			var reasonCount = 0;
			reasonCount += isForbiddenCargo ? 1 : 0;
			reasonCount += isManifestMismatch ? 1 : 0;
			reasonCount += hasHiddenContraband ? 1 : 0;
			reasonCount += hasIllegalMarker ? 1 : 0;

			if (reasonCount == 0)
			{
				return Array.Empty<InspectionErrorReason>();
			}

			var reasons = new InspectionErrorReason[reasonCount];
			var index = 0;

			if (isForbiddenCargo)
			{
				reasons[index++] = InspectionErrorReason.AllowedForbiddenCargo;
			}

			if (isManifestMismatch)
			{
				reasons[index++] = InspectionErrorReason.AllowedManifestMismatch;
			}

			if (hasHiddenContraband)
			{
				reasons[index++] = InspectionErrorReason.AllowedHiddenContraband;
			}

			if (hasIllegalMarker)
			{
				reasons[index] = InspectionErrorReason.AllowedIllegalMarker;
			}

			return reasons;
		}
	}
}
