using System;
using Project.Gameplay.Station.Runtime;
using UnityEngine;

namespace Project.Gameplay.Station.Configs
{
	[Serializable]
	public sealed class NightShiftLetterConfig
	{
		[Tooltip("English heading shown at the top of the shift letter.")]
		public string title = string.Empty;

		[Tooltip("Main English body text of the shift letter.")]
		[TextArea(3, 8)]
		public string body = string.Empty;

		[Tooltip("Optional footer or short hint line shown below the letter body.")]
		public string hint = string.Empty;

		public void ParseConfig()
		{
			title = StationConfigText.Normalize(title);
			body = StationConfigText.Normalize(body);
			hint = StationConfigText.Normalize(hint);
		}

		public NightShiftLetterData ToRuntimeData()
		{
			ParseConfig();
			return new NightShiftLetterData(title, body, hint);
		}
	}
}
