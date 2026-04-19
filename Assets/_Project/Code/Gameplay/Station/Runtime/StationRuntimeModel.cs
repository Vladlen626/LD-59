using System;
using Project.Gameplay.Station.Domain;

namespace Project.Gameplay.Station.Runtime
{
	public sealed class StationRuntimeModel
	{
		public event Action<TrainScenario> ActiveScenarioChanged;
		public event Action<StationFlowState> FlowStateChanged;
		public event Action<TrainTransitState> TrainTransitStateChanged;
		public event Action<bool> CurrentWagonInspectionChanged;
		public event Action<bool> CurrentWagonDoorStateChanged;

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
		public TrainTransitState TrainTransitState { get; private set; } = TrainTransitState.None;
		public int ActiveCaseIndex { get; private set; } = -1;
		public bool HasInspectedCurrentWagon { get; private set; }
		public bool IsCurrentWagonDoorOpen { get; private set; }
		public bool HasActiveScenario => ActiveScenario != null;
		public bool IsTrainInTransit =>
			TrainTransitState == TrainTransitState.Arriving ||
			TrainTransitState == TrainTransitState.Departing;

		public void SetActiveScenario(TrainScenario scenario)
		{
			ResetCurrentWagonState();
			ActiveScenario = scenario;
			ActiveScenarioChanged?.Invoke(ActiveScenario);
		}

		public void ClearActiveScenario()
		{
			ResetCurrentWagonState();
			ActiveScenario = null;
			ActiveScenarioChanged?.Invoke(null);
		}

		public void SetFlowState(StationFlowState flowState)
		{
			if (FlowState == flowState)
			{
				return;
			}

			FlowState = flowState;
			FlowStateChanged?.Invoke(FlowState);
		}

		public void SetTrainTransitState(TrainTransitState trainTransitState)
		{
			if (TrainTransitState == trainTransitState)
			{
				return;
			}

			TrainTransitState = trainTransitState;
			TrainTransitStateChanged?.Invoke(TrainTransitState);
		}

		public void SetActiveCaseIndex(int caseIndex)
		{
			ActiveCaseIndex = Math.Max(-1, caseIndex);
		}

		public void MarkCurrentWagonInspected()
		{
			if (HasInspectedCurrentWagon)
			{
				return;
			}

			HasInspectedCurrentWagon = true;
			CurrentWagonInspectionChanged?.Invoke(true);
		}

		public void SetCurrentWagonDoorOpen(bool isOpen)
		{
			if (IsCurrentWagonDoorOpen == isOpen)
			{
				return;
			}

			IsCurrentWagonDoorOpen = isOpen;
			CurrentWagonDoorStateChanged?.Invoke(IsCurrentWagonDoorOpen);
		}

		public void ResetCurrentWagonInspection()
		{
			if (HasInspectedCurrentWagon == false)
			{
				return;
			}

			HasInspectedCurrentWagon = false;
			CurrentWagonInspectionChanged?.Invoke(false);
		}

		private void ResetCurrentWagonState()
		{
			SetCurrentWagonDoorOpen(false);
			ResetCurrentWagonInspection();
		}
	}
}
