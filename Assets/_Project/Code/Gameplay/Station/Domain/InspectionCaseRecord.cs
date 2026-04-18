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
