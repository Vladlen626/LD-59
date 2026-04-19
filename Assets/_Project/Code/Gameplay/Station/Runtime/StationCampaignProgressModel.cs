using System;
using System.Collections.Generic;

namespace Project.Gameplay.Station.Runtime
{
	public sealed class StationCampaignProgressModel
	{
		private readonly IReadOnlyList<StationDayContent> _days;

		public StationCampaignProgressModel(
			IReadOnlyList<StationDayContent> days,
			int maxCasesPerDay,
			bool allowEndlessAfterCampaign,
			int endlessMinCases,
			int endlessMaxCases)
		{
			_days = days ?? Array.Empty<StationDayContent>();
			MaxCasesPerDay = Math.Max(1, maxCasesPerDay);
			AllowEndlessAfterCampaign = allowEndlessAfterCampaign;
			EndlessMinCases = Math.Max(1, endlessMinCases);
			EndlessMaxCases = Math.Max(EndlessMinCases, endlessMaxCases);
		}

		public int CurrentDayIndex { get; private set; }
		public int TotalDays => _days.Count;
		public int CurrentDayOrder => CurrentDayIndex + 1;
		public int MaxCasesPerDay { get; }
		public bool AllowEndlessAfterCampaign { get; }
		public int EndlessMinCases { get; }
		public int EndlessMaxCases { get; }
		public StationDayContent CurrentDay =>
			CurrentDayIndex >= 0 && CurrentDayIndex < _days.Count
				? _days[CurrentDayIndex]
				: null;

		public bool HasNextDay => CurrentDayIndex + 1 < _days.Count;

		public bool TryAdvanceToNextDay()
		{
			if (!HasNextDay)
			{
				return false;
			}

			CurrentDayIndex++;
			return true;
		}
	}
}
