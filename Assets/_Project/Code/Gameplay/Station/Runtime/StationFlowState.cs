namespace Project.Gameplay.Station.Runtime
{
	public enum StationFlowState
	{
		Inactive = 0,
		StartDay = 1,
		DayIntroPending = 2,
		CallNextTrain = 3,
		BeginInspection = 4,
		AwaitingDecision = 5,
		ResolveCase = 6,
		FinishDay = 7,
		AwaitingTrainCall = 8,
	}
}
