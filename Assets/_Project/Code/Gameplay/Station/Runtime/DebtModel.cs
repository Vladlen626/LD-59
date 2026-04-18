using System;
using Project.Gameplay.Station.Domain;

namespace Project.Gameplay.Station.Runtime
{
	public sealed class DebtModel
	{
		public DebtModel(int initialDebt)
		{
			if (initialDebt < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(initialDebt), "Debt cannot be negative.");
			}

			InitialDebt = initialDebt;
			RemainingDebt = initialDebt;
		}

		public int InitialDebt { get; }
		public int RemainingDebt { get; private set; }
		public int TotalFines { get; private set; }
		public int TotalEarnings { get; private set; }

		public void ApplyInspectionOutcome(InspectionResult result, int rewardOnCorrectDecision)
		{
			if (result == null)
			{
				throw new ArgumentNullException(nameof(result));
			}
			if (rewardOnCorrectDecision < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(rewardOnCorrectDecision), "Reward cannot be negative.");
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
			if (fine < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(fine), "Fine cannot be negative.");
			}

			TotalFines += fine;
			RemainingDebt += fine;
		}

		public void ApplyEarnings(int earnings)
		{
			if (earnings < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(earnings), "Earnings cannot be negative.");
			}

			TotalEarnings += earnings;
			RemainingDebt = Math.Max(0, RemainingDebt - earnings);
		}
	}
}
