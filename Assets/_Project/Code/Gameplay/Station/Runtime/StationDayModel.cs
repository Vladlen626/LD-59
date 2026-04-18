using System;
using Project.Gameplay.Station.Domain;

namespace Project.Gameplay.Station.Runtime
{
	public sealed class StationDayModel
	{
		public StationDayModel(DayRuleSet initialRuleSet)
		{
			SetDay(initialRuleSet);
		}

		public int DayIndex { get; private set; }
		public int CompletedCasesCount { get; private set; }
		public DayRuleSet CurrentRuleSet { get; private set; }

		public void SetDay(DayRuleSet ruleSet)
		{
			if (ruleSet == null)
			{
				throw new ArgumentNullException(nameof(ruleSet));
			}

			CurrentRuleSet = ruleSet;
			DayIndex = ruleSet.DayIndex;
			CompletedCasesCount = 0;
		}

		public void RegisterCompletedCase()
		{
			CompletedCasesCount++;
		}
	}
}
