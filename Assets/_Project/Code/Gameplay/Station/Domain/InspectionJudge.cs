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
			var isForbiddenCargo = HasForbiddenCargo(dayRuleSet, scenario.ActualCargo);
			var isManifestMismatch = scenario.Manifest.DeclaredCargo.Matches(scenario.ActualCargo) == false;

			var hasContraband =
				isForbiddenCargo ||
				isManifestMismatch;

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
				isManifestMismatch);

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
			bool isManifestMismatch)
		{
			if (playerDecision == PlayerDecision.SendBack)
			{
				return RejectedCleanTrainReason;
			}

			var reasonCount = 0;
			reasonCount += isForbiddenCargo ? 1 : 0;
			reasonCount += isManifestMismatch ? 1 : 0;

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

			return reasons;
		}

		private static bool HasForbiddenCargo(DayRuleSet dayRuleSet, CargoLoadout actualCargo)
		{
			if (dayRuleSet == null || actualCargo == null || actualCargo.IsEmpty)
			{
				return false;
			}

			for (var i = 0; i < actualCargo.Entries.Count; i++)
			{
				if (dayRuleSet.IsCargoForbidden(actualCargo.Entries[i].CargoId))
				{
					return true;
				}
			}

			return false;
		}
	}
}
