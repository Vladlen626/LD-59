using UnityEngine;

namespace Project.Gameplay.Scene
{
	public readonly struct SceneRuntimeSetup
	{
		public SceneRuntimeSetup(
			Transform trainSpawnPoint,
			Transform trainSpawnParent)
		{
			TrainSpawnPoint = trainSpawnPoint;
			TrainSpawnParent = trainSpawnParent;
		}

		public Transform TrainSpawnPoint { get; }
		public Transform TrainSpawnParent { get; }
	}
}
