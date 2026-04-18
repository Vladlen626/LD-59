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
			DayIndex = dayIndex;
			MissedContrabandPenalty = Math.Max(0, missedContrabandPenalty);
			WrongfulRejectionPenalty = Math.Max(0, wrongfulRejectionPenalty);
			_forbiddenCargoIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			if (forbiddenCargoIds != null)
			{
				foreach (var cargoId in forbiddenCargoIds)
				{
					if (string.IsNullOrWhiteSpace(cargoId))
					{
						continue;
					}

					_forbiddenCargoIds.Add(cargoId.Trim());
				}
			}
		}

		public int DayIndex { get; }
		public int MissedContrabandPenalty { get; }
		public int WrongfulRejectionPenalty { get; }
		public IReadOnlyCollection<string> ForbiddenCargoIds => _forbiddenCargoIds;

		public bool IsCargoForbidden(string cargoId)
		{
			return string.IsNullOrWhiteSpace(cargoId) == false &&
				_forbiddenCargoIds.Contains(cargoId.Trim());
		}
	}
}
