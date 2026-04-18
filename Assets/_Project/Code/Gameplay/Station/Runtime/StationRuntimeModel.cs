using System;
using Project.Gameplay.Station.Domain;

namespace Project.Gameplay.Station.Runtime
{
	public sealed class StationSharedRuntimeContext
	{
		public StationSharedRuntimeContext(
			StationDayModel stationDay,
			DebtModel debt,
			InspectionJournalModel inspectionJournal)
		{
			StationDay = stationDay ?? throw new ArgumentNullException(nameof(stationDay));
			Debt = debt ?? throw new ArgumentNullException(nameof(debt));
			InspectionJournal = inspectionJournal ?? throw new ArgumentNullException(nameof(inspectionJournal));
			RuntimeModel = new StationRuntimeModel(StationDay, Debt, InspectionJournal);
		}

		public StationDayModel StationDay { get; }
		public DebtModel Debt { get; }
		public InspectionJournalModel InspectionJournal { get; }
		public StationRuntimeModel RuntimeModel { get; }
	}

	public sealed class StationRuntimeModel
	{
		public StationRuntimeModel(
			StationDayModel stationDay,
			DebtModel debt,
			InspectionJournalModel inspectionJournal)
		{
			StationDay = stationDay ?? throw new ArgumentNullException(nameof(stationDay));
			Debt = debt ?? throw new ArgumentNullException(nameof(debt));
			InspectionJournal = inspectionJournal ?? throw new ArgumentNullException(nameof(inspectionJournal));
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
			if (scenario == null)
			{
				return;
			}

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
