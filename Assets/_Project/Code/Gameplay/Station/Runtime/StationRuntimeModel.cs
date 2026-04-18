using System;
using Project.Gameplay.Station.Domain;

namespace Project.Gameplay.Station.Runtime
{
	public sealed class StationRuntimeModel
	{
		public StationRuntimeModel(
			StationDayModel stationDay,
			DebtModel debt,
			InspectionJournalModel inspectionJournal)
		{
			StationDay = stationDay ?? new StationDayModel(new DayRuleSet(1, null, 0, 0));
			Debt = debt ?? new DebtModel(0);
			InspectionJournal = inspectionJournal ?? new InspectionJournalModel();
		}

		public StationDayModel StationDay { get; }
		public DebtModel Debt { get; }
		public InspectionJournalModel InspectionJournal { get; }
		public TrainScenario ActiveScenario { get; private set; }
		public StationFlowState FlowState { get; private set; } = StationFlowState.Inactive;
		public int ActiveCaseIndex { get; private set; } = -1;
		public bool HasActiveScenario => ActiveScenario != null;

		public void SetActiveScenario(TrainScenario scenario)
		{
			ActiveScenario = scenario;
		}

		public void ClearActiveScenario()
		{
			ActiveScenario = null;
		}

		public void SetFlowState(StationFlowState flowState)
		{
			FlowState = flowState;
		}

		public void SetActiveCaseIndex(int caseIndex)
		{
			ActiveCaseIndex = Math.Max(-1, caseIndex);
		}
	}
}
