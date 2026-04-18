using System;

namespace Project.Gameplay.Station.Domain
{
	public sealed class InspectionCaseRecord
	{
		public InspectionCaseRecord(
			int dayIndex,
			int caseIndex,
			TrainScenario scenario,
			InspectionResult result)
		{
			if (scenario == null)
			{
				throw new ArgumentNullException(nameof(scenario));
			}
			if (result == null)
			{
				throw new ArgumentNullException(nameof(result));
			}

			DayIndex = dayIndex;
			CaseIndex = caseIndex;
			Scenario = scenario;
			Result = result;
		}

		public int DayIndex { get; }
		public int CaseIndex { get; }
		public TrainScenario Scenario { get; }
		public InspectionResult Result { get; }
	}
}
