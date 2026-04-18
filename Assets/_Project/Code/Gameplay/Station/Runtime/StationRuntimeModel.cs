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
			StationDay = stationDay ?? throw new ArgumentNullException(nameof(stationDay));
			Debt = debt ?? throw new ArgumentNullException(nameof(debt));
			InspectionJournal = inspectionJournal ?? throw new ArgumentNullException(nameof(inspectionJournal));
		}

		public StationDayModel StationDay { get; }
		public DebtModel Debt { get; }
		public InspectionJournalModel InspectionJournal { get; }
		public TrainScenario ActiveScenario { get; private set; }
		public bool HasActiveScenario => ActiveScenario != null;

		public void SetActiveScenario(TrainScenario scenario)
		{
			if (scenario == null)
			{
				throw new ArgumentNullException(nameof(scenario));
			}

			ActiveScenario = scenario;
		}

		public void ClearActiveScenario()
		{
			ActiveScenario = null;
		}
	}
}
