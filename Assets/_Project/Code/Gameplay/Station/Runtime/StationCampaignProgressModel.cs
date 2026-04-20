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
			int endlessMaxCases,
			int endlessMinOccupiedSlotsPerTrain,
			int endlessMaxOccupiedSlotsPerTrain)
		{
			_days = days ?? Array.Empty<StationDayContent>();
			MaxCasesPerDay = Math.Max(1, maxCasesPerDay);
			AllowEndlessAfterCampaign = allowEndlessAfterCampaign;
			EndlessMinCases = Math.Max(1, endlessMinCases);
			EndlessMaxCases = Math.Max(EndlessMinCases, endlessMaxCases);
			EndlessMinOccupiedSlotsPerTrain = Math.Max(1, endlessMinOccupiedSlotsPerTrain);
			EndlessMaxOccupiedSlotsPerTrain = Math.Max(
				EndlessMinOccupiedSlotsPerTrain,
				endlessMaxOccupiedSlotsPerTrain);
		}

		public int CurrentDayIndex { get; private set; }
		public int TotalDays => _days.Count;
		public int CurrentDayOrder => IsEndlessMode ? TotalDays + EndlessDayOrder : CurrentDayIndex + 1;
		public int MaxCasesPerDay { get; }
		public bool AllowEndlessAfterCampaign { get; }
		public int EndlessMinCases { get; }
		public int EndlessMaxCases { get; }
		public int EndlessMinOccupiedSlotsPerTrain { get; }
		public int EndlessMaxOccupiedSlotsPerTrain { get; }
		public bool IsEndlessMode { get; private set; }
		public int EndlessDayOrder { get; private set; }
		public StationDayContent CurrentDay
		{
			get
			{
				if (IsEndlessMode)
				{
					return _endlessDay;
				}

				return CurrentDayIndex >= 0 && CurrentDayIndex < _days.Count
					? _days[CurrentDayIndex]
					: null;
			}
		}

		public bool HasNextDay => CurrentDayIndex + 1 < _days.Count;
		public bool IsCurrentDayLastAuthored => IsEndlessMode == false && _days.Count > 0 && CurrentDayIndex == _days.Count - 1;
		public bool CanStartEndlessMode => AllowEndlessAfterCampaign && IsCurrentDayLastAuthored;

		private StationDayContent _endlessDay;

		public bool TryAdvanceToNextDay()
		{
			if (!HasNextDay)
			{
				return false;
			}

			CurrentDayIndex++;
			return true;
		}

		public bool StartEndlessMode(StationDayContent firstEndlessDay)
		{
			if (firstEndlessDay == null || CanStartEndlessMode == false)
			{
				return false;
			}

			IsEndlessMode = true;
			EndlessDayOrder = 1;
			_endlessDay = firstEndlessDay;
			return true;
		}

		public bool SetNextEndlessDay(StationDayContent nextEndlessDay)
		{
			if (nextEndlessDay == null || IsEndlessMode == false)
			{
				return false;
			}

			EndlessDayOrder++;
			_endlessDay = nextEndlessDay;
			return true;
		}
	}
}
