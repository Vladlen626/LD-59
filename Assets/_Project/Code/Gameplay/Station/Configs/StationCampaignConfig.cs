using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Gameplay.Station.Configs
{
	[Serializable]
	public sealed class StationCampaignConfig : BaseConfig
	{
		[Tooltip("Ordered day resource paths used for authored campaign progression.")]
		public List<string> day_resource_paths = new List<string>();

		[Tooltip("Unlock endless mode after campaign is completed.")]
		public bool allow_endless_after_campaign;

		[Tooltip("Global hard cap for cases loaded into one day.")]
		[Min(1)]
		public int max_cases_per_day = StationDayConfig.MaxTrainScenariosPerDay;

		[Tooltip("Minimum cases for future endless day generation.")]
		[Min(1)]
		public int endless_min_cases = 6;

		[Tooltip("Maximum cases for future endless day generation.")]
		[Min(1)]
		public int endless_max_cases = StationDayConfig.MaxTrainScenariosPerDay;

		public override void ParseConfig()
		{
			max_cases_per_day = Mathf.Clamp(max_cases_per_day, 1, StationDayConfig.MaxTrainScenariosPerDay);
			endless_min_cases = Mathf.Clamp(endless_min_cases, 1, max_cases_per_day);
			endless_max_cases = Mathf.Clamp(endless_max_cases, endless_min_cases, max_cases_per_day);

			if (day_resource_paths == null)
			{
				day_resource_paths = new List<string>();
				return;
			}

			var normalizedPaths = new List<string>(day_resource_paths.Count);
			for (var i = 0; i < day_resource_paths.Count; i++)
			{
				var normalizedPath = StationConfigText.Normalize(day_resource_paths[i]);
				if (string.IsNullOrEmpty(normalizedPath))
				{
					continue;
				}

				normalizedPaths.Add(normalizedPath);
			}

			day_resource_paths = normalizedPaths;
		}
	}
}
