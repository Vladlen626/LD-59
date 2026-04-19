using System;
using System.Collections.Generic;
using Project.Gameplay.Station.Domain;

namespace Project.Gameplay.Station.Runtime
{
	public sealed class StationDayContent
	{
		public StationDayContent(
			string resourcePath,
			int initialDebt,
			DayRuleSet ruleSet,
			IReadOnlyList<TrainScenario> scenarios)
		{
			ResourcePath = string.IsNullOrWhiteSpace(resourcePath) ? string.Empty : resourcePath.Trim();
			InitialDebt = Math.Max(0, initialDebt);
			RuleSet = ruleSet ?? new DayRuleSet(1, null, 0, 0);
			Scenarios = scenarios ?? Array.Empty<TrainScenario>();
		}

		public string ResourcePath { get; }
		public int InitialDebt { get; }
		public DayRuleSet RuleSet { get; }
		public IReadOnlyList<TrainScenario> Scenarios { get; }
	}
}
