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
		public int OutstandingDebt => Math.Max(0, RemainingDebt);
		public bool IsCleared => RemainingDebt <= 0;

		private bool _isDaySettled;

		public void Reset(int initialDebt)
		{
			var normalizedInitialDebt = Math.Max(0, initialDebt);
			InitialDebt = normalizedInitialDebt;
			RemainingDebt = normalizedInitialDebt;
			StartNewDay();
		}

		public void StartNewDay()
		{
			TotalFines = 0;
			TotalEarnings = 0;
			TotalLivingCosts = 0;
			_isDaySettled = false;
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
		}

		public void ApplyEarnings(int earnings)
		{
			if (earnings <= 0)
			{
				return;
			}

			TotalEarnings += earnings;
		}

		public void ApplyLivingCost(int livingCost)
		{
			if (livingCost <= 0)
			{
				return;
			}

			TotalLivingCosts += livingCost;
		}

		public void SettleDay(int livingCost)
		{
			if (_isDaySettled)
			{
				return;
			}

			ApplyLivingCost(livingCost);
			var netResult = NetResult;
			if (netResult >= 0)
			{
				RemainingDebt = Math.Max(0, RemainingDebt - netResult);
			}
			else
			{
				RemainingDebt += -netResult;
			}

			_isDaySettled = true;
		}
	}
}
