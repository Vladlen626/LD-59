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
			StationDay = stationDay ?? new StationDayModel(new DayRuleSet(1, null, 0, 0));
			Debt = debt ?? new DebtModel(0);
			InspectionJournal = inspectionJournal ?? new InspectionJournalModel();
			RuntimeModel = new StationRuntimeModel(StationDay, Debt, InspectionJournal);
		}

		public StationDayModel StationDay { get; }
		public DebtModel Debt { get; }
		public InspectionJournalModel InspectionJournal { get; }
		public StationRuntimeModel RuntimeModel { get; }
	}
}
