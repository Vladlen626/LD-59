using Project.Gameplay.Station.Domain;

namespace Project.Gameplay.Station.Runtime
{
	public sealed class StationDayModel
	{
		public StationDayModel(DayRuleSet initialRuleSet)
		{
			CurrentRuleSet = initialRuleSet ?? new DayRuleSet(1, null, 0, 0);
			DayIndex = CurrentRuleSet.DayIndex;
		}

		public int DayIndex { get; private set; }
		public int CompletedCasesCount { get; private set; }
		public DayRuleSet CurrentRuleSet { get; private set; }

		public void SetDay(DayRuleSet ruleSet)
		{
			if (ruleSet == null)
			{
				return;
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
