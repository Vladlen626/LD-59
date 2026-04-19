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
			IReadOnlyList<TrainScenario> scenarios,
			NightShiftLetterData shiftLetter,
			NightShiftLetterData finalNotice,
			DayLivingCostData livingCost,
			string endDayNote)
		{
			ResourcePath = string.IsNullOrWhiteSpace(resourcePath) ? string.Empty : resourcePath.Trim();
			InitialDebt = Math.Max(0, initialDebt);
			RuleSet = ruleSet ?? new DayRuleSet(1, null, 0, 0);
			Scenarios = scenarios ?? Array.Empty<TrainScenario>();
			ShiftLetter = shiftLetter ?? new NightShiftLetterData(string.Empty, string.Empty, string.Empty);
			FinalNotice = finalNotice ?? new NightShiftLetterData(string.Empty, string.Empty, string.Empty);
			LivingCost = livingCost ?? new DayLivingCostData(string.Empty, 0);
			EndDayNote = string.IsNullOrWhiteSpace(endDayNote) ? string.Empty : endDayNote.Trim();
		}

		public string ResourcePath { get; }
		public int InitialDebt { get; }
		public DayRuleSet RuleSet { get; }
		public IReadOnlyList<TrainScenario> Scenarios { get; }
		public NightShiftLetterData ShiftLetter { get; }
		public NightShiftLetterData FinalNotice { get; }
		public DayLivingCostData LivingCost { get; }
		public string EndDayNote { get; }
	}
}
