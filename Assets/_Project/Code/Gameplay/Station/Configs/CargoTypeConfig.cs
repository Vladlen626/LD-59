using System;
using UnityEngine;

namespace Project.Gameplay.Station.Configs
{
	[Serializable]
	public sealed class CargoTypeConfig : BaseConfig
	{
		[Tooltip("Human-readable cargo name for UI and logs.")]
		public string display_name = string.Empty;

		[Tooltip("Resources path to prefab used for this cargo type.")]
		public string prefab_resource_path = string.Empty;

		public override void ParseConfig()
		{
			id = StationConfigText.Normalize(id);
			display_name = StationConfigText.Normalize(display_name);
			prefab_resource_path = StationConfigText.Normalize(prefab_resource_path);
		}
	}
}
