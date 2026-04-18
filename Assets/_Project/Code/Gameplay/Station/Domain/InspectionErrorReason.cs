namespace Project.Gameplay.Station.Domain
{
	public enum InspectionErrorReason
	{
		AllowedForbiddenCargo = 0,
		AllowedManifestMismatch = 1,
		AllowedHiddenContraband = 2,
		AllowedIllegalMarker = 3,
		RejectedCleanTrain = 4,
	}
}
