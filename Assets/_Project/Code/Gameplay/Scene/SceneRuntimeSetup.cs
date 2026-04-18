using System.Collections.Generic;
using Project.Gameplay.Station.Domain;
using UnityEngine;

namespace Project.Gameplay.Scene
{
	public readonly struct SceneRuntimeSetup
	{
		public SceneRuntimeSetup(
			DayRuleSet dayRuleSet,
			int initialDebt,
			List<TrainScenario> scenarios,
			Transform trainSpawnPoint,
			Transform trainSpawnParent)
		{
			DayRuleSet = dayRuleSet;
			InitialDebt = Mathf.Max(0, initialDebt);
			Scenarios = scenarios ?? new List<TrainScenario>();
			TrainSpawnPoint = trainSpawnPoint;
			TrainSpawnParent = trainSpawnParent;
		}

		public DayRuleSet DayRuleSet { get; }
		public int InitialDebt { get; }
		public List<TrainScenario> Scenarios { get; }
		public Transform TrainSpawnPoint { get; }
		public Transform TrainSpawnParent { get; }
	}
}
