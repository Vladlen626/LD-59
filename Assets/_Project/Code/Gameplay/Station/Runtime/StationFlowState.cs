namespace Project.Gameplay.Station.Runtime
{
	public enum StationFlowState
	{
		Inactive = 0,
		StartDay = 1,
		CallNextTrain = 2,
		BeginInspection = 3,
		AwaitingDecision = 4,
		ResolveCase = 5,
		FinishDay = 6,
		AwaitingTrainCall = 7,
	}
}
