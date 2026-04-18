using System;
using System.Collections.Generic;

namespace Project.Gameplay.Station.Domain
{
	public sealed class InspectionResult
	{
		private readonly IReadOnlyList<InspectionErrorReason> _errorReasons;

		public InspectionResult(
			InspectionVerdict verdict,
			PlayerDecision playerDecision,
			PlayerDecision expectedDecision,
			int penalty,
			IReadOnlyList<InspectionErrorReason> errorReasons)
		{
			Verdict = verdict;
			PlayerDecision = playerDecision;
			ExpectedDecision = expectedDecision;
			Penalty = Math.Max(0, penalty);
			_errorReasons = errorReasons ?? Array.Empty<InspectionErrorReason>();
		}

		public InspectionVerdict Verdict { get; }
		public PlayerDecision PlayerDecision { get; }
		public PlayerDecision ExpectedDecision { get; }
		public int Penalty { get; }
		public IReadOnlyList<InspectionErrorReason> ErrorReasons => _errorReasons;
		public bool IsCorrect => Verdict == InspectionVerdict.Correct;
	}
}
