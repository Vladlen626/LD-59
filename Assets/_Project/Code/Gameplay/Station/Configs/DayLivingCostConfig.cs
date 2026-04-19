using System;
using Project.Gameplay.Station.Runtime;
using UnityEngine;

namespace Project.Gameplay.Station.Configs
{
	[Serializable]
	public sealed class DayLivingCostConfig
	{
		[Tooltip("Short label shown in the end-of-shift settlement.")]
		public string label = "Food and lamp oil";

		[Tooltip("How much the player pays after the shift for basic living costs.")]
		[Min(0)]
		public int amount;

		public void ParseConfig()
		{
			label = StationConfigText.Normalize(label);
			if (amount < 0)
			{
				amount = 0;
			}
		}

		public DayLivingCostData ToRuntimeData()
		{
			ParseConfig();
			return new DayLivingCostData(label, amount);
		}
	}
}
