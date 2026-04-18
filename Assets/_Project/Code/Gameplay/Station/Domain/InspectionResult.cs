using System;
using System.Collections.Generic;
using System.Linq;

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
			if (penalty < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(penalty), "Penalty cannot be negative.");
			}
			if (errorReasons == null)
			{
				throw new ArgumentNullException(nameof(errorReasons));
			}

			Verdict = verdict;
			PlayerDecision = playerDecision;
			ExpectedDecision = expectedDecision;
			Penalty = penalty;
			_errorReasons = errorReasons.ToArray();
		}

		public InspectionVerdict Verdict { get; }
		public PlayerDecision PlayerDecision { get; }
		public PlayerDecision ExpectedDecision { get; }
		public int Penalty { get; }
		public IReadOnlyList<InspectionErrorReason> ErrorReasons => _errorReasons;
		public bool IsCorrect => Verdict == InspectionVerdict.Correct;
	}
}
