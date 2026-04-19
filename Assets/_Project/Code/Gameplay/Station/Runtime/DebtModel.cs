using System;
using Project.Gameplay.Station.Domain;

namespace Project.Gameplay.Station.Runtime
{
	public sealed class DebtModel
	{
		public DebtModel(int initialDebt)
		{
			Reset(initialDebt);
		}

		public int InitialDebt { get; private set; }
		public int RemainingDebt { get; private set; }
		public int TotalFines { get; private set; }
		public int TotalEarnings { get; private set; }
		public int TotalLivingCosts { get; private set; }
		public int NetResult => TotalEarnings - TotalFines - TotalLivingCosts;

		public void Reset(int initialDebt)
		{
			var normalizedInitialDebt = Math.Max(0, initialDebt);
			InitialDebt = normalizedInitialDebt;
			RemainingDebt = normalizedInitialDebt;
			TotalFines = 0;
			TotalEarnings = 0;
			TotalLivingCosts = 0;
		}

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

		public void ApplyLivingCost(int livingCost)
		{
			if (livingCost <= 0)
			{
				return;
			}

			TotalLivingCosts += livingCost;
			RemainingDebt += livingCost;
		}
	}
}
