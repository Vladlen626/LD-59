using System;
using Project.Gameplay.Station.Domain;

namespace Project.Gameplay.Station.Runtime
{
	public sealed class DebtModel
	{
		public DebtModel(int initialDebt)
		{
			var normalizedInitialDebt = Math.Max(0, initialDebt);
			InitialDebt = normalizedInitialDebt;
			RemainingDebt = normalizedInitialDebt;
		}

		public int InitialDebt { get; }
		public int RemainingDebt { get; private set; }
		public int TotalFines { get; private set; }
		public int TotalEarnings { get; private set; }

		public void ApplyInspectionOutcome(InspectionResult result, int rewardOnCorrectDecision)
		{
			if (result == null)
			{
				return;
			}

			if (result.IsCorrect)
			{
				ApplyEarnings(rewardOnCorrectDecision);
				return;
			}

			ApplyFine(result.Penalty);
		}

		public void ApplyFine(int fine)
		{
			if (fine <= 0)
			{
				return;
			}

			TotalFines += fine;
			RemainingDebt += fine;
		}

		public void ApplyEarnings(int earnings)
		{
			if (earnings <= 0)
			{
				return;
			}

			TotalEarnings += earnings;
			RemainingDebt = Math.Max(0, RemainingDebt - earnings);
		}
	}
}
