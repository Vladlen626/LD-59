using System;
using System.Collections.Generic;

namespace Project.Gameplay.Station.Domain
{
	public sealed class DayRuleSet
	{
		private readonly HashSet<string> _forbiddenCargoIds;

		public DayRuleSet(
			int dayIndex,
			IEnumerable<string> forbiddenCargoIds,
			int missedContrabandPenalty,
			int wrongfulRejectionPenalty)
		{
			if (dayIndex < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(dayIndex), "Day index must be greater than zero.");
			}
			if (missedContrabandPenalty < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(missedContrabandPenalty), "Penalty cannot be negative.");
			}
			if (wrongfulRejectionPenalty < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(wrongfulRejectionPenalty), "Penalty cannot be negative.");
			}

			DayIndex = dayIndex;
			MissedContrabandPenalty = missedContrabandPenalty;
			WrongfulRejectionPenalty = wrongfulRejectionPenalty;
			_forbiddenCargoIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			if (forbiddenCargoIds == null)
			{
				return;
			}

			foreach (var cargoId in forbiddenCargoIds)
			{
				if (string.IsNullOrWhiteSpace(cargoId))
				{
					continue;
				}

				_forbiddenCargoIds.Add(cargoId.Trim());
			}
		}

		public int DayIndex { get; }
		public int MissedContrabandPenalty { get; }
		public int WrongfulRejectionPenalty { get; }
		public IReadOnlyCollection<string> ForbiddenCargoIds => _forbiddenCargoIds;

		public bool IsCargoForbidden(string cargoId)
		{
			if (string.IsNullOrWhiteSpace(cargoId))
			{
				return false;
			}

			return _forbiddenCargoIds.Contains(cargoId.Trim());
		}
	}
}
