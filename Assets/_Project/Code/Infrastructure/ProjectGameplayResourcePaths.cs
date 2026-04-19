namespace Project.Infrastructure
{
	public static class ProjectGameplayResourcePaths
	{
		// Keep train path centralized and avoid string literals in runtime controllers.
		public const string CargoClues_HiddenContraband = "CargoClues/HiddenContrabandClue";
		public const string CargoClues_IllegalMarker = "CargoClues/IllegalMarkerClue";
		public const string Trains_CargoWagon = "Trains/CargoWagon";
		public const string Json_cargo_types = "Json/cargo_types";
		public const string Json_station_campaign = "Json/station_campaign";
		public const string Json_station_day = "Json/station_day";
	}
}
